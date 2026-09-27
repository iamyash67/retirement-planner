import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { AddContributionRequest, Contribution, CreateGoalRequest, Goal, GoalProgress } from '../models/goals.model';

/** The signed-in user's goals. The API identifies the user from the access token, never from the URL. */
@Injectable({ providedIn: 'root' })
export class GoalsService {
  private readonly baseUrl = `${API_BASE_URL}/goals`;

  constructor(private http: HttpClient) {}

  listGoals(): Observable<Goal[]> {
    return this.http.get<Goal[]>(this.baseUrl);
  }

  getGoal(goalId: number): Observable<Goal> {
    return this.http.get<Goal>(`${this.baseUrl}/${goalId}`);
  }

  createGoal(goal: CreateGoalRequest): Observable<Goal> {
    return this.http.post<Goal>(this.baseUrl, goal);
  }

  addContribution(goalId: number, contribution: AddContributionRequest): Observable<Contribution> {
    return this.http.post<Contribution>(`${this.baseUrl}/${goalId}/contributions`, contribution);
  }

  getProgress(goalId: number): Observable<GoalProgress> {
    return this.http.get<GoalProgress>(`${this.baseUrl}/${goalId}/progress`);
  }
}
