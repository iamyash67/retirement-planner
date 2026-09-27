import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Goals } from '../models/goals.model';

@Injectable({
  providedIn: 'root',
})
export class GoalsService {
  private baseUrl = 'http://localhost:5294/api/goal';
  private financialBaseUrl = 'http://localhost:5294/api/financial';

  // Caching the goal data
  private goalSubject = new BehaviorSubject<Goals | null>(null);
  goal$ = this.goalSubject.asObservable();

  constructor(private http: HttpClient) {}

  // GET api/goal/{profileId}
  getGoalByProfileId(profileId: number): Observable<Goals> {
    return this.http.get<Goals>(`${this.baseUrl}/${profileId}`).pipe(
      tap((goal) => this.goalSubject.next(goal)) // cache on success
    );
  }

  // Set cached goal manually (used in ProfileComponent)
  setGoal(goal: Goals): void {
    this.goalSubject.next(goal);
  }

  // Get cached goal
  getCachedGoal(): Goals | null {
    return this.goalSubject.value;
  }

  // Clear cached goal (optional for logout, etc.)
  clearCachedGoal(): void {
    this.goalSubject.next(null);
  }

  // POST api/goal
  createGoal(goal: Partial<Goals>): Observable<string> {
    return this.http.post(`${this.baseUrl}`, goal, { responseType: 'text' });
  }

  // POST api/financial/Add-Investment
  addInvestment(investment: {
    goalId: number;
    year: number;
    month: number;
    monthlyInvestment: number;
  }): Observable<Goals> {
    return this.http.post<Goals>(`${this.financialBaseUrl}/Add-Investment`, investment);
  }

  // GET api/financial/progress/{goalId}
  getGoalProgress(goalId: number): Observable<{ goalId: number; progress: string }> {
  return this.http.get<{ goalId: number; progress: string }>(
    `${this.financialBaseUrl}/progress/${goalId}`
  );
  }
}
