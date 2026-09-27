import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GoalsService } from '../services/goals.service';
import { AuthService } from '../services/auth.service';
import { Goals } from '../models/goals.model';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ActivatedRoute } from '@angular/router';
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
  goal: Goals | null = null;
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
    monthlyInvestment: 0,
  };

  investmentSuccess = '';
  investmentError = '';
  investmentFieldErrors: FieldErrors = {};

  constructor(
    private goalService: GoalsService,
    private authService: AuthService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const cachedGoal = this.goalService.getCachedGoal();

    if (cachedGoal) {
      this.goal = cachedGoal;
      this.errorMessage = '';
      this.isLoading = false;
    } else {
      const profileIdStr = this.route.snapshot.paramMap.get('profileId');
      const profileId = Number(profileIdStr);

      if (!profileId || isNaN(profileId)) {
        this.errorMessage = 'Profile ID not found in route.';
        this.isLoading = false;
        return;
      }

      this.goalService.getGoalByProfileId(profileId).subscribe({
        next: (goal) => {
          this.goal = goal;
          this.goalService.setGoal(goal); // cache it for reuse
          this.errorMessage = '';
          this.isLoading = false;
        },
        error: (err) => {
          this.goal = null;
          this.isLoading = false;
          if (err.status === 404) {
            this.errorMessage = 'No goal found for your profile.';
          } else {
            this.errorMessage = 'Failed to load goal data.';
          }
        },
      });
    }
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
      this.goalService.getGoalProgress(this.goal.goalId).subscribe({
        next: (data) => {
          this.progressPercentage = data.progress || '0%';
          this.progressLoading = false;
        },
        error: (err) => {
          this.progressError = 'Failed to load progress.';
          this.progressLoading = false;
        },
      });
    }
  }

  onSubmit(): void {
    const profile = this.authService.getProfile();
    this.createFieldErrors = {};
    if (!profile) {
      this.createError = 'Profile not found.';
      this.createSuccess = '';
      return;
    }

    const goalPayload = {
      profileId: profile.profileId,
      currentAge: this.newGoal.currentAge,
      retirementAge: this.newGoal.retirementAge,
      targetSavings: this.newGoal.targetSavings,
      currentSavings: this.newGoal.currentSavings,
    };

    this.goalService.createGoal(goalPayload).subscribe({
      next: (goal) => {
        this.createSuccess = 'Goal created successfully';
        this.createError = '';
        this.goal = goal;
        this.goalService.setGoal(goal); // cache the goal the API returned
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

    const payload = {
      goalId: this.goal.goalId,
      year: this.addInvestmentData.year,
      month: this.addInvestmentData.month,
      monthlyInvestment: this.addInvestmentData.monthlyInvestment,
    };

    this.goalService.addInvestment(payload).subscribe({
      next: (updatedGoal) => {
        this.goal = updatedGoal;
        this.goalService.setGoal(updatedGoal); // update cache
        this.investmentSuccess = 'Investment recorded successfully.';
        this.investmentError = '';
        setTimeout(() => {
          this.selectedSection = 'none';
        }, 3000);
      },
      error: (err) => {
        const { fieldErrors, message } = splitFieldErrors(
          parseApiError(err, 'Failed to record investment.'),
          ['year', 'month', 'monthlyInvestment']
        );
        this.investmentSuccess = '';
        this.investmentFieldErrors = fieldErrors;
        this.investmentError = message;
      },
    });
  }

  fetchGoal(): void {
    const profile = this.authService.getProfile();
    if (!profile) {
      this.errorMessage = 'Profile not found. Please login.';
      this.isLoading = false;
      return;
    }

    this.goalService.getGoalByProfileId(profile.profileId).subscribe({
      next: (goal) => {
        this.goal = goal;
        this.goalService.setGoal(goal); // update cache
        this.errorMessage = '';
        this.isLoading = false;
      },
      error: (err) => {
        this.goal = null;
        this.isLoading = false;
        if (err.status === 404) {
          this.errorMessage = 'No goal found for your profile.';
        } else {
          this.errorMessage = 'Failed to load goal data.';
        }
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
