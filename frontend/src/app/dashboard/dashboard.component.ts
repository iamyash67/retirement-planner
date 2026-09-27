import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button'
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { GoalsService } from '../services/goals.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule,MatButtonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
  standalone: true
})
export class DashboardComponent {
  constructor(private router: Router, private authService: AuthService, private goalsService: GoalsService) {}

  showProfile() {
    const profile = this.authService.getCurrentUser();
    if (profile) {
      this.router.navigate(['/dashboard/profile'], {
        queryParams: { profileId: profile.profileId },
        replaceUrl: true
      });
    }
  }

  toggleGoals() {
    const profile = this.authService.getCurrentUser();
    if (profile) {
      this.router.navigate(['/dashboard/goals', profile.profileId]);
    }
  }

  logout() {
    this.authService.logout();
    localStorage.clear();
    sessionStorage.clear();
    this.goalsService.clearCachedGoal();
    this.router.navigateByUrl('/login');
  }
}