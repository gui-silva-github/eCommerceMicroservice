import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthenticationResponse } from '../../models/authentication-response';
import { UsersService } from '../../services/users.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import {
  applyApiFieldErrors,
  clearApiFieldErrors,
  getApiError,
  getApiErrorMessage,
  hasApiError,
  parseApiError,
} from '../../utils/api-error.util';

@Component({
  selector: 'app-login',
  imports: [CommonModule, MatFormFieldModule, MatInputModule, ReactiveFormsModule, MatCardModule, MatButtonModule, MatIconModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  readonly errorMessage = signal<string | null>(null);
  readonly isSubmitting = signal(false);

  loginForm: FormGroup;

  constructor(private fb: FormBuilder, private usersService: UsersService, private router: Router) {
    this.loginForm = this.fb.group({
      Email: ['', [Validators.required, Validators.email]],
      Password: ['', Validators.required],
    });
  }

  login(): void {
    if (!this.loginForm.valid || this.isSubmitting()) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    clearApiFieldErrors(this.loginForm);

    const { Email, Password } = this.loginForm.value;

    this.usersService.login(Email, Password).subscribe({
      next: (response: AuthenticationResponse) => {
        this.isSubmitting.set(false);

        if (response.success) {
          const isAdmin = response.userID === 'admin_id';
          this.usersService.setAuthStatus(response.token, isAdmin, response.personName);
          this.router.navigate(['/products', 'showcase']);
          return;
        }

        this.errorMessage.set('Email e/ou senha inválidos.');
      },
      error: (error) => {
        this.isSubmitting.set(false);

        const parsed = parseApiError(error);
        applyApiFieldErrors(this.loginForm, parsed.errors);
        this.errorMessage.set(getApiErrorMessage(error, 'Não foi possível realizar o login.'));
      },
    });
  }

  get emailFormControl(): FormControl {
    return this.loginForm.get('Email') as FormControl;
  }

  get passwordFormControl(): FormControl {
    return this.loginForm.get('Password') as FormControl;
  }

  protected readonly hasApiError = hasApiError;
  protected readonly getApiError = getApiError;
}
