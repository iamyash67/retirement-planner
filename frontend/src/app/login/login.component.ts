import { Component, OnInit, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { FormGroup, FormControl, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { FieldErrors, parseApiError, splitFieldErrors } from '../services/api-errors';

// Angular Material Modules
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  standalone: true,
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ]
})
export class LoginComponent implements OnInit {
  loginForm = new FormGroup({
    username: new FormControl('', Validators.required),
    password: new FormControl('', Validators.required)
  });

  errorMessage = '';
  fieldErrors: FieldErrors = {};
  isLoading = false;
  isBrowser: boolean;

  constructor(
    private authService: AuthService,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  ngOnInit(): void {
  }

  onSubmit() {
    if (this.loginForm.invalid) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.fieldErrors = {};

    const UserName = this.loginForm.get('username')?.value ?? '';
    const Password = this.loginForm.get('password')?.value ?? '';

    this.authService.login({ UserName, Password }).subscribe({
      next: (profile) => {
        this.isLoading = false;

        this.authService.setCurrentUser(profile);
        localStorage.setItem('currentUser', JSON.stringify(profile));

        const profileId = profile.profileId;

        if (profileId) {
          this.router.navigate(['/dashboard/profile'], {
            queryParams: { profileId },
            replaceUrl: true, // Prevent back to login
          });
        } else {
          this.errorMessage = 'Login successful but profile ID is missing.';
        }
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 401) {
          this.errorMessage = 'Invalid username or password.';
        } else {
          // The API names the fields userName and password.
          const { fieldErrors, message } = splitFieldErrors(
            parseApiError(err, 'Login failed. Please try again later.'),
            ['userName', 'password']
          );
          this.fieldErrors = fieldErrors;
          this.errorMessage = message;
        }
        console.error('Login error:', err);
      }
    });
  }

  navigateToHome() {
    this.router.navigate(['/']);
  }

  navigateToLogin() {
    this.router.navigate(['/login']);
  }
}
