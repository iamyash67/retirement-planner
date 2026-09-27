import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { API_BASE_URL, AUTH_URL } from '../api.config';
import { AuthResponse } from '../models/user.model';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: AuthService;
  let router: jasmine.SpyObj<Router>;

  const authResponse = (accessToken: string): AuthResponse => ({
    accessToken,
    expiresAt: '2026-09-27T10:15:00Z',
    user: { id: 1, email: 'demo@example.com', firstName: 'Demo', lastName: 'User', age: 30, gender: null },
  });

  function signIn(accessToken: string): void {
    auth.login('demo@example.com', 'demo123').subscribe();
    backend.expectOne(`${AUTH_URL}/login`).flush(authResponse(accessToken));
  }

  beforeEach(() => {
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => backend.verify());

  it('sends auth requests with credentials (for the cookie) and without the access token', () => {
    signIn('token-1');

    auth.refresh().subscribe();
    const refresh = backend.expectOne(`${AUTH_URL}/refresh`);

    expect(refresh.request.withCredentials).toBeTrue();
    expect(refresh.request.headers.has('Authorization')).toBeFalse();
    refresh.flush(authResponse('token-2'));
  });

  it('attaches the access token to API requests', () => {
    signIn('token-1');

    http.get(`${API_BASE_URL}/goals`).subscribe();
    const req = backend.expectOne(`${API_BASE_URL}/goals`);

    expect(req.request.headers.get('Authorization')).toBe('Bearer token-1');
    expect(req.request.withCredentials).toBeFalse();
    req.flush([]);
  });

  it('does not touch requests to other hosts', () => {
    signIn('token-1');

    http.get('https://example.com/data').subscribe();
    const req = backend.expectOne('https://example.com/data');

    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('refreshes silently on a 401 and retries with the new token', () => {
    signIn('expired-token');
    let result: unknown;

    http.get(`${API_BASE_URL}/goals`).subscribe((goals) => (result = goals));
    backend.expectOne(`${API_BASE_URL}/goals`).flush('', { status: 401, statusText: 'Unauthorized' });
    backend.expectOne(`${AUTH_URL}/refresh`).flush(authResponse('fresh-token'));
    const retry = backend.expectOne(`${API_BASE_URL}/goals`);

    expect(retry.request.headers.get('Authorization')).toBe('Bearer fresh-token');
    retry.flush([{ id: 1 }]);
    expect(result).toEqual([{ id: 1 }]);
    expect(auth.accessToken()).toBe('fresh-token');
  });

  it('shares one refresh between concurrent 401s, because the API rejects a reused refresh token', () => {
    signIn('expired-token');

    http.get(`${API_BASE_URL}/goals`).subscribe();
    http.get(`${API_BASE_URL}/goals/1/progress`).subscribe();
    backend.expectOne(`${API_BASE_URL}/goals`).flush('', { status: 401, statusText: 'Unauthorized' });
    backend.expectOne(`${API_BASE_URL}/goals/1/progress`).flush('', { status: 401, statusText: 'Unauthorized' });

    const refreshes = backend.match(`${AUTH_URL}/refresh`);
    expect(refreshes.length).toBe(1);
    refreshes[0].flush(authResponse('fresh-token'));

    backend.expectOne(`${API_BASE_URL}/goals`).flush([]);
    backend.expectOne(`${API_BASE_URL}/goals/1/progress`).flush({ goalId: 1, progress: '0%' });
  });

  it('signs out and goes to the login page when the refresh fails', () => {
    signIn('expired-token');
    let status = 0;

    http.get(`${API_BASE_URL}/goals`).subscribe({ error: (err) => (status = err.status) });
    backend.expectOne(`${API_BASE_URL}/goals`).flush('', { status: 401, statusText: 'Unauthorized' });
    backend.expectOne(`${AUTH_URL}/refresh`).flush('Invalid or expired refresh token', { status: 401, statusText: 'Unauthorized' });

    expect(status).toBe(401);
    expect(auth.isAuthenticated()).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('retries only once: a second 401 goes to the caller', () => {
    signIn('token-1');
    let status = 0;

    http.get(`${API_BASE_URL}/goals`).subscribe({ error: (err) => (status = err.status) });
    backend.expectOne(`${API_BASE_URL}/goals`).flush('', { status: 401, statusText: 'Unauthorized' });
    backend.expectOne(`${AUTH_URL}/refresh`).flush(authResponse('token-2'));
    backend.expectOne(`${API_BASE_URL}/goals`).flush('', { status: 401, statusText: 'Unauthorized' });

    expect(status).toBe(401);
    backend.expectNone(`${AUTH_URL}/refresh`);
  });

  it('passes other errors straight through without refreshing', () => {
    signIn('token-1');
    let status = 0;

    http.get(`${API_BASE_URL}/goals/9`).subscribe({ error: (err) => (status = err.status) });
    backend.expectOne(`${API_BASE_URL}/goals/9`).flush('Goal not found', { status: 404, statusText: 'Not Found' });

    expect(status).toBe(404);
    backend.expectNone(`${AUTH_URL}/refresh`);
  });
});
