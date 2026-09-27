import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, finalize, firstValueFrom, map, of, shareReplay, tap } from 'rxjs';
import { AUTH_URL } from '../api.config';
import { AuthResponse, RegisterRequest, User } from '../models/user.model';

interface Session {
  accessToken: string;
  user: User;
}

/**
 * Holds the signed-in session in memory only: nothing is written to localStorage or sessionStorage.
 * The access token lives in this service; the refresh token lives in an httpOnly cookie that scripts
 * can't read, and a page reload restores the session by calling /auth/refresh (see restoreSession).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly session = signal<Session | null>(null);
  private refreshInFlight$: Observable<string> | null = null;

  readonly user = computed(() => this.session()?.user ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);

  constructor(private http: HttpClient) {}

  accessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  login(email: string, password: string): Observable<User> {
    return this.http.post<AuthResponse>(`${AUTH_URL}/login`, { email, password }).pipe(
      tap((response) => this.startSession(response)),
      map((response) => response.user)
    );
  }

  register(request: RegisterRequest): Observable<User> {
    return this.http.post<AuthResponse>(`${AUTH_URL}/register`, request).pipe(
      tap((response) => this.startSession(response)),
      map((response) => response.user)
    );
  }

  /**
   * Exchanges the refresh cookie for a new access token. Concurrent callers share one request: the API
   * rotates the refresh token on every use and treats a second use of the same token as theft.
   */
  refresh(): Observable<string> {
    this.refreshInFlight$ ??= this.http.post<AuthResponse>(`${AUTH_URL}/refresh`, null).pipe(
      tap({
        next: (response) => this.startSession(response),
        error: () => this.session.set(null),
      }),
      map((response) => response.accessToken),
      finalize(() => (this.refreshInFlight$ = null)),
      shareReplay({ bufferSize: 1, refCount: false })
    );
    return this.refreshInFlight$;
  }

  /** Called once at startup: signs the user back in if the refresh cookie is still valid. */
  restoreSession(): Promise<void> {
    return firstValueFrom(
      this.refresh().pipe(
        map(() => undefined),
        catchError(() => of(undefined))
      )
    );
  }

  /** Revokes the refresh token on the server and forgets the session, even if the request fails. */
  logout(): Observable<void> {
    return this.http.post<void>(`${AUTH_URL}/logout`, null).pipe(
      catchError(() => of(undefined)),
      map(() => undefined),
      finalize(() => this.session.set(null))
    );
  }

  clearSession(): void {
    this.session.set(null);
  }

  private startSession(response: AuthResponse): void {
    this.session.set({ accessToken: response.accessToken, user: response.user });
  }
}
