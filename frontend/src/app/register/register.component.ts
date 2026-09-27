import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../services/auth.service';
import { FieldErrors, parseApiError, splitFieldErrors } from '../services/api-errors';

@Component({
  standalone: true,
  selector: 'app-register',
  templateUrl: './register.component.html',
  // Same card layout as the login page.
  styleUrls: ['../login/login.component.css'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
})
export class RegisterComponent {
  /** Mirrors the API's rules so most mistakes are caught before submitting; the API has the final say. */
  registerForm = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(8)]),
    firstName: new FormControl('', Validators.required),
    lastName: new FormControl('', Validators.required),
    dateOfBirth: new FormControl('', Validators.required),
    gender: new FormControl(''),
  });

  errorMessage = '';
  fieldErrors: FieldErrors = {};
  isLoading = false;

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit(): void {
    if (this.registerForm.invalid) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.fieldErrors = {};
    const value = this.registerForm.getRawValue();

    this.authService
      .register({
        email: value.email ?? '',
        password: value.password ?? '',
        firstName: value.firstName ?? '',
        lastName: value.lastName ?? '',
        dateOfBirth: value.dateOfBirth ?? '',
        gender: value.gender || null,
      })
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.router.navigate(['/dashboard/goals'], { replaceUrl: true });
        },
        error: (err) => {
          this.isLoading = false;
          if (err.status === 429) {
            this.errorMessage = 'Too many attempts. Please wait a minute and try again.';
            return;
          }
          const { fieldErrors, message } = splitFieldErrors(
            parseApiError(err, 'Registration failed. Please try again later.'),
            ['email', 'password', 'firstName', 'lastName', 'dateOfBirth', 'gender']
          );
          this.fieldErrors = fieldErrors;
          this.errorMessage = message;
        },
      });
  }
}
