import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { ProfileComponent } from './profile/profile.component';
import { GoalsComponent } from './goals/goals.component';
import { AuthGuard } from './services/auth.guard';

export const routes: Routes = [
  {
    path: 'dashboard',
    canActivate: [AuthGuard], // protect the dashboard
    children: [
      {
        path: 'profile',
        component: ProfileComponent,
      },
      {
        path: 'goals/:profileId',
        component: GoalsComponent,
      }
    ]
  },
  { path: 'login', component: LoginComponent },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
];

