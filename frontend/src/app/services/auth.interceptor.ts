import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE_URL, AUTH_URL } from '../api.config';
import { AuthService } from './auth.service';

/**
 * - Auth endpoints (login, register, refresh, logout) are sent with credentials so the browser stores
 *   and sends the httpOnly refresh cookie. They never carry the access token.
 * - Every other API request gets "Authorization: Bearer <access token>".
 * - On a 401 the interceptor refreshes once (shared with any other request doing the same) and retries;
 *   if the refresh fails the session is over and the user is sent to the login page.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(API_BASE_URL)) {
    return next(req);
  }

  if (req.url.startsWith(`${AUTH_URL}/`)) {
    return next(req.clone({ withCredentials: true }));
  }

  const auth = inject(AuthService);
  const router = inject(Router);

  return next(withAccessToken(req, auth.accessToken())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        catchError(() => {
          auth.clearSession();
          router.navigate(['/login']);
          return throwError(() => error);
        }),
        // Retried once: a second 401 is passed to the caller rather than refreshing again.
        switchMap((token) => next(withAccessToken(req, token)))
      );
    })
  );
};

function withAccessToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}
