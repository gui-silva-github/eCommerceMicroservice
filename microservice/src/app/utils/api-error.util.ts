import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup } from '@angular/forms';
import { ApiErrorResponse } from '../models/api-error-response';

export interface ParsedApiError extends ApiErrorResponse {
  status?: number;
}

export function parseApiError(error: unknown): ParsedApiError {
  if (error instanceof HttpErrorResponse) {
    const body = error.error;

    if (body && typeof body === 'object' && 'message' in body) {
      const apiError = body as ApiErrorResponse;
      return {
        message: apiError.message ?? '',
        errors: apiError.errors ?? null,
        type: apiError.type ?? null,
        status: error.status,
      };
    }

    if (typeof body === 'string' && body.trim()) {
      return { message: body, errors: null, type: null, status: error.status };
    }

    if (error.status === 0) {
      return {
        message: 'Não foi possível conectar ao servidor. Verifique se a API está ativa.',
        errors: null,
        type: null,
        status: 0,
      };
    }

    if (error.status === 401) {
      return {
        message: 'Email e/ou senha inválidos.',
        errors: null,
        type: null,
        status: 401,
      };
    }
  }

  return { message: '', errors: null, type: null };
}

export function getApiErrorMessage(error: unknown, fallback = 'Ocorreu um erro. Tente novamente.'): string {
  const parsed = parseApiError(error);
  return parsed.message?.trim() || fallback;
}

export function applyApiFieldErrors(
  form: FormGroup,
  errors: Record<string, string[]> | null | undefined,
  fieldMap: Record<string, string> = {},
): void {
  if (!errors) {
    return;
  }

  for (const [apiField, messages] of Object.entries(errors)) {
    if (!messages.length) {
      continue;
    }

    const controlName = fieldMap[apiField] ?? apiField;
    const control = form.get(controlName);

    if (control) {
      control.setErrors({ ...control.errors, apiError: messages.join(' ') });
      control.markAsTouched();
    }
  }
}

export function clearApiFieldErrors(form: FormGroup): void {
  for (const key of Object.keys(form.controls)) {
    const control = form.get(key);

    if (!control?.errors?.['apiError']) {
      continue;
    }

    const { apiError: _removed, ...remainingErrors } = control.errors;
    control.setErrors(Object.keys(remainingErrors).length ? remainingErrors : null);
  }
}

export function hasApiError(control: { hasError: (name: string) => boolean }): boolean {
  return control.hasError('apiError');
}

export function getApiError(control: { getError: (name: string) => unknown }): string {
  return String(control.getError('apiError') ?? '');
}
