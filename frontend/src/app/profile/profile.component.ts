import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from '../services/auth.service';
import { DashboardComponent } from '../dashboard/dashboard.component';

@Component({
  standalone: true,
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'],
  imports: [CommonModule, MatCardModule, DashboardComponent],
})
export class ProfileComponent {
  /** The signed-in user, from the in-memory auth state. */
  readonly user = inject(AuthService).user;
}
