import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { switchMap } from 'rxjs';
import { GoalsService } from '../services/goals.service';
import { Goal } from '../models/goals.model';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { DashboardComponent } from '../dashboard/dashboard.component';
import { FieldErrors, parseApiError, splitFieldErrors } from '../services/api-errors';

@Component({
  standalone: true,
  selector: 'app-goals',
  templateUrl: './goals.component.html',
  styleUrls: ['./goals.component.css'],
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressBarModule,
    DashboardComponent
  ],
})
export class GoalsComponent implements OnInit {
  goal: Goal | null = null;
  errorMessage = '';
  isLoading = true;

  selectedSection: 'none' | 'add' | 'progress' = 'none';

  progressPercentage = '';
  progressLoading = false;
  progressError = '';

  newGoal = {
    currentAge: 0,
    retirementAge: 0,
    targetSavings: 0,
    currentSavings: 0,
  };

  createSuccess = '';
  createError = '';
  createFieldErrors: FieldErrors = {};

  addInvestmentData = {
    year: new Date().getFullYear(),
    month: new Date().getMonth() + 1,
    amount: 0,
  };

  investmentSuccess = '';
  investmentError = '';
  investmentFieldErrors: FieldErrors = {};

  constructor(private goalService: GoalsService) {}

  ngOnInit(): void {
    this.loadGoal();
  }

  /** The API allows one goal per user for now, so the page shows the first goal in the user's list. */
  loadGoal(): void {
    this.isLoading = true;
    this.goalService.listGoals().subscribe({
      next: (goals) => {
        this.goal = goals[0] ?? null;
        this.errorMessage = this.goal ? '' : 'No goal found for your profile.';
        this.isLoading = false;
      },
      error: () => {
        this.goal = null;
        this.errorMessage = 'Failed to load goal data.';
        this.isLoading = false;
      },
    });
  }

  toggleSection(section: 'add' | 'progress'): void {
    this.selectedSection = this.selectedSection === section ? 'none' : section;

    // Reset messages whenever section toggled
    this.investmentSuccess = '';
    this.investmentError = '';
    this.investmentFieldErrors = {};
    this.progressError = '';
    this.progressPercentage = '';

    if (section === 'progress' && this.goal) {
      this.progressLoading = true;
      this.goalService.getProgress(this.goal.id).subscribe({
        next: (data) => {
          this.progressPercentage = data.progress || '0%';
          this.progressLoading = false;
        },
        error: () => {
          this.progressError = 'Failed to load progress.';
          this.progressLoading = false;
        },
      });
    }
  }

  onSubmit(): void {
    this.createFieldErrors = {};

    this.goalService.createGoal({ ...this.newGoal }).subscribe({
      next: (goal) => {
        this.createSuccess = 'Goal created successfully';
        this.createError = '';
        this.goal = goal;
        this.errorMessage = '';
        this.selectedSection = 'none'; // hide any open sections on create
      },
      error: (err) => {
        const { fieldErrors, message } = splitFieldErrors(
          parseApiError(err, 'Failed to create goal.'),
          ['currentAge', 'retirementAge', 'targetSavings', 'currentSavings']
        );
        this.createFieldErrors = fieldErrors;
        this.createError = message;
        this.createSuccess = '';
      },
    });
  }

  onAddInvestment(): void {
    if (!this.goal) return;
    this.investmentFieldErrors = {};
    const goalId = this.goal.id;

    // The API returns the new contribution; the goal is re-read to show the updated savings.
    this.goalService
      .addContribution(goalId, { ...this.addInvestmentData })
      .pipe(switchMap(() => this.goalService.getGoal(goalId)))
      .subscribe({
        next: (updatedGoal) => {
          this.goal = updatedGoal;
          this.investmentSuccess = 'Investment recorded successfully.';
          this.investmentError = '';
          setTimeout(() => {
            this.selectedSection = 'none';
          }, 3000);
        },
        error: (err) => {
          const { fieldErrors, message } = splitFieldErrors(
            parseApiError(err, 'Failed to record investment.'),
            ['year', 'month', 'amount']
          );
          this.investmentSuccess = '';
          this.investmentFieldErrors = fieldErrors;
          this.investmentError = message;
        },
      });
  }

  parseProgressValue(progress: string): number {
    if (!progress) return 0;
    // Remove % sign and convert to number
    const numeric = Number(progress.replace('%', ''));
    return isNaN(numeric) ? 0 : numeric;
  }
}
