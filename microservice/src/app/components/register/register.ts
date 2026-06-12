import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthenticationResponse } from '../../models/authentication-response';
import { UsersService } from '../../services/users.service';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatCardModule } from '@angular/material/card';
import { Router, RouterModule } from '@angular/router';
import { MatOptionModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import {
  applyApiFieldErrors,
  clearApiFieldErrors,
  getApiError,
  getApiErrorMessage,
  hasApiError,
  parseApiError,
} from '../../utils/api-error.util';

@Component({
  selector: 'app-register',
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatIconModule, MatToolbarModule,
    RouterModule, MatCardModule, MatOptionModule, MatFormFieldModule, MatInputModule, MatSelectModule,
  ],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  readonly errorMessage = signal<string | null>(null);
  readonly isSubmitting = signal(false);

  registerForm: FormGroup;

  constructor(private fb: FormBuilder, private usersService: UsersService, private router: Router) {
    this.registerForm = this.fb.group({
      PersonName: ['', Validators.required],
      Email: ['', [Validators.required, Validators.email]],
      Password: ['', [Validators.required, Validators.minLength(3)]],
      Gender: [''],
    });
  }

  register(): void {
    if (!this.registerForm.valid || this.isSubmitting()) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    clearApiFieldErrors(this.registerForm);

    this.usersService.register(this.registerForm.value).subscribe({
      next: (response: AuthenticationResponse) => {
        this.isSubmitting.set(false);
        this.usersService.setAuthStatus(response.token, false, response.personName);
        this.router.navigate(['/products', 'showcase']);
      },
      error: (error) => {
        this.isSubmitting.set(false);

        const parsed = parseApiError(error);
        applyApiFieldErrors(this.registerForm, parsed.errors);
        this.errorMessage.set(getApiErrorMessage(error, 'Não foi possível concluir o cadastro.'));
      },
    });
  }

  get emailFormControl(): FormControl {
    return this.registerForm.get('Email') as FormControl;
  }

  get passwordFormControl(): FormControl {
    return this.registerForm.get('Password') as FormControl;
  }

  get personNameFormControl(): FormControl {
    return this.registerForm.get('PersonName') as FormControl;
  }

  get genderFormControl(): FormControl {
    return this.registerForm.get('Gender') as FormControl;
  }

  protected readonly hasApiError = hasApiError;
  protected readonly getApiError = getApiError;
}
