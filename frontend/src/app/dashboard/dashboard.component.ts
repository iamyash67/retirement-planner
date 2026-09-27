import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, MatButtonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
  standalone: true
})
export class DashboardComponent {
  constructor(private router: Router, private authService: AuthService) {}

  showProfile() {
    this.router.navigate(['/dashboard/profile']);
  }

  showGoals() {
    this.router.navigate(['/dashboard/goals']);
  }

  /** Revokes the refresh token on the server, forgets the in-memory session and returns to login. */
  logout() {
    this.authService.logout().subscribe(() => this.router.navigateByUrl('/login', { replaceUrl: true }));
  }
}
