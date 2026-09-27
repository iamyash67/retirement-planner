import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Profile } from '../models/profile.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private baseUrl = 'http://localhost:5294/api/user';
  private currentUser: Profile | null = null;

  constructor(private http: HttpClient) {}

  private isBrowser(): boolean {
    return typeof window !== 'undefined' && typeof localStorage !== 'undefined';
  }

  login(credentials: { UserName: string; Password: string }): Observable<Profile> {
    return this.http.post<Profile>(`${this.baseUrl}/login`, credentials).pipe(
      tap(profile => {
        this.currentUser = profile;

        if (this.isBrowser()) {
          localStorage.setItem('currentProfile', JSON.stringify(profile));
          localStorage.setItem('currentUser', JSON.stringify(profile));
        }
      })
    );
  }

  logout(): void {
    this.currentUser = null;
    if (this.isBrowser()) {
      localStorage.removeItem('currentProfile');
      localStorage.removeItem('currentUser');
    }
  }

  getProfile(): Profile | null {
    if (this.currentUser) return this.currentUser;

    if (this.isBrowser()) {
      const storedProfile = localStorage.getItem('currentProfile');
      return storedProfile ? JSON.parse(storedProfile) : null;
    }

    return null;
  }

  setCurrentUser(user: Profile): void {
    this.currentUser = user;
    if (this.isBrowser()) {
      localStorage.setItem('currentUser', JSON.stringify(user));
    }
  }

  getCurrentUser(): Profile | null {
    if (this.currentUser) return this.currentUser;

    if (this.isBrowser()) {
      const userData = localStorage.getItem('currentUser');
      return userData ? JSON.parse(userData) : null;
    }

    return null;
  }
}

