import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { AUTH_URL } from '../api.config';
import { AuthResponse } from '../models/user.model';

describe('AuthService', () => {
  let auth: AuthService;
  let backend: HttpTestingController;

  const response: AuthResponse = {
    accessToken: 'access-token',
    expiresAt: '2026-09-27T10:15:00Z',
    user: { id: 1, email: 'demo@example.com', firstName: 'Demo', lastName: 'User', age: 30, gender: null },
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    auth = TestBed.inject(AuthService);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('keeps the session in memory only, never in web storage', () => {
    const setItem = spyOn(Storage.prototype, 'setItem').and.callThrough();

    auth.login('demo@example.com', 'demo123').subscribe();
    backend.expectOne(`${AUTH_URL}/login`).flush(response);

    expect(auth.isAuthenticated()).toBeTrue();
    expect(auth.user()?.email).toBe('demo@example.com');
    expect(auth.accessToken()).toBe('access-token');
    expect(setItem).not.toHaveBeenCalled();
  });

  it('restores the session from the refresh cookie at startup', async () => {
    const restored = auth.restoreSession();
    backend.expectOne(`${AUTH_URL}/refresh`).flush(response);
    await restored;

    expect(auth.isAuthenticated()).toBeTrue();
  });

  it('starts signed out when there is no valid refresh cookie', async () => {
    const restored = auth.restoreSession();
    backend.expectOne(`${AUTH_URL}/refresh`).flush('Invalid or expired refresh token', { status: 401, statusText: 'Unauthorized' });
    await restored;

    expect(auth.isAuthenticated()).toBeFalse();
    expect(auth.accessToken()).toBeNull();
  });

  it('forgets the session on logout, even if the request fails', () => {
    auth.login('demo@example.com', 'demo123').subscribe();
    backend.expectOne(`${AUTH_URL}/login`).flush(response);

    auth.logout().subscribe();
    backend.expectOne(`${AUTH_URL}/logout`).flush('', { status: 500, statusText: 'Server Error' });

    expect(auth.isAuthenticated()).toBeFalse();
  });
});
