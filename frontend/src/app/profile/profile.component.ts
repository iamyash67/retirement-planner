import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth.service';
import { Profile } from '../models/profile.model';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { GoalsComponent } from '../goals/goals.component';
import { ActivatedRoute, Router } from '@angular/router';
import { GoalsService } from '../services/goals.service';
import { DashboardComponent } from '../dashboard/dashboard.component';
@Component({
  standalone: true,
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'],
  imports: [
    CommonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatButtonModule,
    GoalsComponent,
    DashboardComponent
],
})
export class ProfileComponent implements OnInit {
  profile?: Profile;
  isLoading = true;
  errorMessage = '';
  showGoals = false;

  constructor(
    private authService: AuthService,
    private goalsService: GoalsService, // Inject GoalsService
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const profileIdParam = params['profileId'];
      const currentUser = this.authService.getCurrentUser();

      if (profileIdParam && currentUser) {
        const profileId = Number(profileIdParam);

        if (currentUser.profileId === profileId) {
          this.profile = currentUser;
          this.errorMessage = '';

          // Call once and cache the goal data
          this.goalsService.getGoalByProfileId(profileId).subscribe({
            next: goal => {
              this.goalsService.setGoal(goal); // cache the goal
              this.isLoading = false;
            },
            error: err => {
              this.isLoading = false;
              this.errorMessage = 'Failed to fetch goal data.';
              console.error('Goal fetch error:', err);
            }
          });
        } else {
          this.errorMessage = 'Profile ID mismatch. Please login again.';
          this.isLoading = false;
        }
      } else {
        this.errorMessage = 'No profile found or Profile ID missing.';
        this.isLoading = false;
      }
    });
  }

}
