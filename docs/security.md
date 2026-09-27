# Security decisions

These are the deliberate choices behind authentication in Retirement Planner. Each one says what was decided,
why, and what it costs. Settings live in `appsettings.json` under `Jwt` and `RateLimiting`; the signing key
lives only in user-secrets or the `Jwt__SigningKey` environment variable.

## Token lifetimes

| Token | Lifetime | Setting |
|---|---|---|
| Access token (JWT, HS256) | 15 minutes | `Jwt:AccessTokenMinutes` |
| Refresh token | 7 days from its last rotation | `Jwt:RefreshTokenDays` |

- **Access tokens are short-lived because they can't be revoked.** The API checks their signature and expiry
  but keeps no list of issued tokens. A stolen access token works until it expires, so the lifetime is the
  exposure window. Validation allows 30 seconds of clock skew instead of the library default of 5 minutes,
  which would quietly stretch 15 minutes to 20.
- **Refresh tokens are revocable because they're checked in the database on every use.** That's why they can
  live for days: logout and reuse detection end them immediately.
- **Refresh expiry slides.** Every refresh issues a new token with a fresh 7-day expiry, so an active user
  stays signed in, and a session idle for 7 days ends. There is no absolute maximum session length yet.
- **The signing key is required at startup.** The API refuses to start if `Jwt:SigningKey` is missing or
  shorter than 32 bytes, so it can never run with a weak or default key. Only HS256 is accepted, and issuer
  and audience are validated.

## Refresh tokens are stored hashed

A refresh token is 32 random bytes (base64url). The database stores only its SHA-256 hash, so a copy of
the `RefreshTokens` table, whether from a backup, a log or a SQL injection elsewhere, can't be used to sign
in as anyone.

A plain SHA-256 is enough here, unlike for passwords. Password hashing is deliberately slow (PBKDF2 via
`PasswordHasher`) because passwords are low-entropy and can be guessed. A 256-bit random token can't be
guessed, so a work factor would add nothing but cost to every refresh. Hashing also makes lookups exact:
the hash has a unique index.

## Cookie flags and path

The refresh token travels only in the `rp_refresh` cookie, never in a response body:

| Flag | Why |
|---|---|
| `HttpOnly` | Page scripts can't read it, so an XSS bug can't steal the long-lived token. |
| `Secure` | Sent only over HTTPS. Browsers make an exception for `http://localhost` during development. |
| `SameSite=Strict` | Not sent on requests started by other sites, which blocks cross-site request forgery of refresh and logout. |
| `Path=/api/auth` | Sent only to the refresh and logout endpoints, not on every API call. |
| `Expires` | Matches the token's expiry, so the browser drops it when it's useless. |

The access token is kept in memory by the Angular app, never in `localStorage` or `sessionStorage`. A
reload loses it, and the app gets a new one from the cookie at startup. CORS allows credentials only for
the SPA's origin (`http://localhost:4200`); other origins get no CORS headers.

## Rotation, reuse detection and the grace period

Every refresh revokes the presented token, records its successor in `ReplacedByTokenId`, and issues a new
token in the same *family* (one family per login). A token that is presented again after it has been
revoked means someone is replaying it. We can't tell whether that's the attacker or the real user, so the
whole family is revoked and that login has to sign in again. Other logins (other families) are untouched.

**Grace period.** Two tabs, or two requests from one tab, can refresh with the same cookie at the same
moment. The row lock lets exactly one of them rotate the token, and the other then sees a revoked token.
Without special handling, that race looked like theft and signed the user out everywhere. So:

- If the presented token was **revoked by rotation** (it has a `ReplacedByTokenId`) no more than
  `Jwt:RefreshTokenReuseGraceSeconds` ago (default **10 seconds**), the request gets a plain 401 and
  **the family is kept**. The winner's new token keeps working and is now in the shared cookie. The
  current frontend still treats the loser's 401 as "signed out": that tab goes to the login page, and
  reloading it signs it straight back in from the cookie. Retrying the refresh once in the frontend would
  hide this; that's a possible follow-up.
- Reuse **after** the grace period is treated as theft: the family is revoked.
- A token **revoked by logout or by an earlier family revocation** has no replacement, so it never gets
  grace. Presenting it always revokes the family.
- Setting the grace period to `0` restores strict reuse detection.

**Trade-off.** During those 10 seconds a replay is refused but not punished. If an attacker holding a
copy of the token refreshes *before* the real client, the real client's refresh lands in the grace window
and gets a 401 instead of triggering revocation, while the attacker keeps the rotated session until it
idles out. The user notices they were signed out and signs in again (a new family), but the stolen family
isn't killed. We accept this narrow window in exchange for not signing users out whenever two tabs
refresh together. Detecting it would need signals we don't collect yet (client binding, a "sign out
everywhere" action, alerting on grace-period hits).

## Rate limiting per IP

Login, register and refresh share one fixed-window limit: **10 requests per minute per client IP**
(`RateLimiting:Auth`). Beyond it the API returns 429 ProblemDetails with `Retry-After`. Other endpoints
aren't limited, because they already need a valid access token.

This slows online password guessing, bulk registration, email enumeration (below) and refresh-token
guessing. Its limits:

- **It's per IP, not per account.** A distributed attack from many addresses isn't stopped, and users
  behind one NAT share a budget. There is no per-account lockout, which would itself let an attacker lock
  users out.
- **Behind a reverse proxy** the forwarded-headers middleware must be configured. Otherwise every client
  appears to have the proxy's IP and they all share one budget.
- **Counters are in memory, per API instance.** They reset on restart and aren't shared across instances.

## Register returns 409 for a taken email

`POST /api/auth/register` answers **409 "Email is already registered"** when the email exists. This is a
deliberate usability choice: someone who forgot they have an account learns why sign-up failed and can go
to log in instead.

The cost is **account enumeration**: anyone can learn whether an email has an account. That's mitigated,
not prevented:

- The per-IP rate limit makes bulk probing slow.
- Login does *not* leak it: a wrong password and an unknown email get the same 401 "Invalid email or
  password".
- Revisit this if accounts become sensitive. The enumeration-safe alternative is to always answer "check
  your email", then send either a verification link or an "you already have an account" message. That
  needs email delivery, which the app doesn't have yet.
