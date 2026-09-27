# Retirement Planner

A web app for planning retirement savings. Users register or log in, set a retirement goal (current age,
retirement age, target amount and current savings), record monthly investments, and track how
close they are to the target.

When a goal is created, the app works out the monthly contribution needed to close the gap by
the retirement age. Each recorded investment is added to current savings, and progress is shown
as a percentage of the target. Each month can be recorded only once.

## Architecture

```
Angular SPA  ──HTTP/JSON──▶  ASP.NET Core Web API  ──Dapper──▶  MySQL (schema via DbUp migrations)
```

The API is split into layers, each with one responsibility:

| Layer | Folder | Responsibility |
|---|---|---|
| Controllers | `Controllers/` | Translate HTTP to service calls and results to status codes; no business logic |
| Validators | `Validators/` | One FluentValidation validator per request DTO, run before every action |
| Mapping | `Mapping/` | Explicit extension methods between DTOs and domain models |
| Services | `Services/` | Business logic; coordinates repositories |
| Repositories | `Repositories/` | Data access with Dapper and parameterised SQL |
| Data | `Data/` | Connection factory, unit of work, migrator and development seeder |
| DTOs | `DTO/Requests`, `DTO/Responses` | The API contract: one request and one response type per endpoint |
| Models | `Models/` | Domain entities and commands used by services and repositories; never returned by the API |

Services and repositories are defined by interfaces (`Services/Interfaces`, `Repositories/Interfaces`)
and registered with dependency injection in `Program.cs`, so each layer depends on abstractions
rather than concrete classes.

All repositories in a request share one connection and, when a service needs it, one transaction,
through a scoped `IUnitOfWork`.

Every request DTO has a FluentValidation validator. A global action filter runs it before the action,
and failures return **400 ValidationProblemDetails** with one entry per field, keyed by the JSON field
name (`errors.retirementAge`). The Angular forms show these messages under the matching inputs.
Not-found (404), conflict (409) and bad-login (401) responses stay plain strings. Only `IDbConnectionFactory` creates connections. Unhandled exceptions
are returned as RFC 7807 ProblemDetails by a global exception handler. See
[docs/database.md](docs/database.md) for the schema, an ER diagram and the design decisions.

The Angular frontend uses standalone components (login, register, profile, goals, dashboard header)
and services that call the API. An HTTP interceptor attaches the access token and refreshes it silently
on a 401, and route guards use the in-memory auth state to protect the dashboard pages.

## Tech stack

- **Backend:** ASP.NET Core 10 Web API, Dapper, MySqlConnector, DbUp, Swagger (Swashbuckle)
- **Frontend:** Angular 18, Angular Material, RxJS
- **Database:** MySQL 8.4 in Docker; schema managed by versioned DbUp migrations
- **Tests:** xUnit, Moq, Testcontainers (MySQL)
- **Tooling:** .NET 10 SDK (pinned in `backend/global.json`), Node 20 (`frontend/.nvmrc`), Docker Compose

## Project structure

```
backend/
  RetirementPlanner.sln
  global.json                   .NET SDK pin
  RetirementPlanner.Api/
    Controllers/                Auth and Goals endpoints
    Auth/                       JWT options and issuing, refresh-token cookie and hashing, auth setup
    Services/                   Business logic (+ Interfaces/)
    Repositories/               Data access with Dapper (+ Interfaces/)
    Data/                       Connection factory, unit of work, migrator, dev seeder (+ Interfaces/)
    Migrations/                 Versioned SQL migrations (V001__..., embedded, run on startup)
    Infrastructure/             Global exception handler (ProblemDetails)
    Validators/                 FluentValidation validators, one per request DTO
    Mapping/                    DTO <-> domain mapping extension methods
    DTO/                        Requests/ and Responses/ (the API contract)
    Models/                     Domain models
    Program.cs                  DI registration, migrations, CORS, Swagger
  RetirementPlanner.Tests/
    Services/ Infrastructure/   Unit tests with mocked repositories
    Integration/                Repository and migration tests against MySQL in Testcontainers
docker/mysql-init/              Creates the empty database on first container start
docs/database.md                Schema, ER diagram and design decisions
frontend/
  src/app/
    login/ register/ profile/ goals/ dashboard/   Components
    services/                           API clients, auth service, interceptor and guards
    models/                             TypeScript interfaces
docker-compose.yml              MySQL service
.env.example                    Template for local database passwords
```

## Setup

Prerequisites: .NET 10 SDK, Node 20, Docker Desktop. You do not need a local MySQL server.

### 1. Database (MySQL 8.4 in Docker, host port 3307)

```bash
cp .env.example .env    # then set real passwords in .env
docker compose up -d
docker compose ps       # wait until the mysql service shows "healthy"
```

On first start the container creates the empty `retirement_planner` database and the
`retirement_planner_app` user from `.env`. The API creates the tables itself: it applies any pending
migrations from `Migrations/` on startup. The data lives in the `mysql-data` volume. To start from an
empty database: `docker compose down -v && docker compose up -d`.

Compose publishes the container's port 3306 on host port **3307**, so it does not clash with a
local MySQL server on 3306.

### 2. Backend (http://localhost:5294, Swagger at `/swagger`)

```bash
cd backend/RetirementPlanner.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Port=3307;Database=retirement_planner;User=retirement_planner_app;Password=<MYSQL_PASSWORD from .env>;"
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
dotnet run --launch-profile http
```

The JWT signing key is required: the API refuses to start without one (at least 32 bytes). Outside
development, set it with the environment variable `Jwt__SigningKey`. Never put it in `appsettings.json`.

In the Development environment the API also seeds a demo user on startup.

### 3. Frontend (http://localhost:4200)

```bash
cd frontend
nvm use
npm install
npm start
```

Log in with `demo@example.com` / `demo123`, or create an account on the register page.

The refresh-token cookie is `Secure`. Browsers accept that over plain HTTP only for `localhost`, so use
`http://localhost:4200` and not `http://127.0.0.1:4200` or a LAN address.

### Tests

```bash
cd backend
dotnet test    # unit tests, plus integration tests that start MySQL with Testcontainers (Docker must be running)
```

## Authentication

- **Passwords** are hashed with ASP.NET Core's `PasswordHasher`.
- **Access tokens** are JWTs (HS256) that live for 15 minutes and are sent as `Authorization: Bearer <token>`.
  The frontend keeps them in memory only; nothing goes into `localStorage` or `sessionStorage`.
- **Refresh tokens** are random values sent in an httpOnly, Secure, SameSite=Strict cookie scoped to
  `/api/auth`. Only their SHA-256 hash is stored.
  - Each refresh rotates the token.
  - Presenting an already-used token revokes that login's whole token family. The exception is a token
    rotated within the last 10 seconds (`Jwt:RefreshTokenReuseGraceSeconds`): two tabs refreshing at once
    is a race, not theft, so the later request gets a 401 but the family is kept.
  - After a page reload, the frontend restores the session by calling `/api/auth/refresh`.
- **Every endpoint except `/api/auth/*` requires an access token.** The user id comes only from the token,
  never from the route or the body. Another user's goal returns 404.
- **Rate limit:** login, register and refresh share a limit of 10 requests per minute per client IP
  (`RateLimiting:Auth`). Over the limit, they return 429 with `Retry-After`.

See [docs/security.md](docs/security.md) for the reasoning and trade-offs behind each of these decisions.

## API

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | – | Create an account and sign in; 201 with access token, sets the refresh cookie |
| POST | `/api/auth/login` | – | Sign in with email and password; 200 with access token, sets the refresh cookie |
| POST | `/api/auth/refresh` | cookie | Rotate the refresh cookie and return a new access token |
| POST | `/api/auth/logout` | cookie | Revoke the refresh token family and clear the cookie; 204 |
| GET | `/api/goals` | bearer | The current user's goals |
| POST | `/api/goals` | bearer | Create a goal (one per user for now); 201 with the goal |
| GET | `/api/goals/{id}` | bearer | One of the current user's goals (404 for anyone else's) |
| POST | `/api/goals/{id}/contributions` | bearer | Record a month's contribution; 201, 409 if the month is already recorded |
| GET | `/api/goals/{id}/progress` | bearer | Percentage of the target saved |
