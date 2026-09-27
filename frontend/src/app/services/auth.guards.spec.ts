import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { authGuard, guestGuard } from './auth.guards';
import { AuthService } from './auth.service';
import { AUTH_URL } from '../api.config';

describe('auth guards', () => {
  const run = (guard: typeof authGuard) =>
    TestBed.runInInjectionContext(() => guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
  });

  function signIn(): void {
    TestBed.inject(AuthService).login('demo@example.com', 'demo123').subscribe();
    TestBed.inject(HttpTestingController).expectOne(`${AUTH_URL}/login`).flush({
      accessToken: 't', expiresAt: '', user: { id: 1, email: 'demo@example.com', firstName: 'D', lastName: 'U', age: 30, gender: null },
    });
  }

  it('authGuard sends signed-out users to /login', () => {
    const result = run(authGuard) as UrlTree;

    expect(result.toString()).toBe('/login');
  });

  it('authGuard lets signed-in users through', () => {
    signIn();

    expect(run(authGuard)).toBeTrue();
  });

  it('guestGuard sends signed-in users to their goals', () => {
    signIn();

    expect((run(guestGuard) as UrlTree).toString()).toBe('/dashboard/goals');
  });

  it('guestGuard lets signed-out users see login and register', () => {
    expect(run(guestGuard)).toBeTrue();
  });
});
