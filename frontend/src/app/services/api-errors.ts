import { HttpErrorResponse } from '@angular/common/http';

/** First message per field, keyed by the request's camelCase field name (e.g. "retirementAge"). */
export type FieldErrors = Record<string, string>;

export interface ApiError {
  /** A message that isn't tied to a field: a plain-string body (401/404/409) or the fallback. */
  message: string;
  fieldErrors: FieldErrors;
}

/**
 * Reads an API error. A 400 ValidationProblemDetails becomes per-field messages; a plain-string
 * body (the API's 401, 404 and 409 responses) becomes the message; anything else uses the fallback.
 */
export function parseApiError(err: HttpErrorResponse, fallback: string): ApiError {
  const body = err.error;

  if (typeof body === 'string' && body.trim()) {
    return { message: body, fieldErrors: {} };
  }

  if (err.status === 400 && body && typeof body === 'object' && body.errors && typeof body.errors === 'object') {
    const fieldErrors: FieldErrors = {};
    for (const [field, messages] of Object.entries(body.errors as Record<string, string[]>)) {
      if (Array.isArray(messages) && messages.length > 0) {
        fieldErrors[field] = messages[0];
      }
    }
    return { message: '', fieldErrors };
  }

  return { message: fallback, fieldErrors: {} };
}

/**
 * Splits field errors into those a form shows under its inputs and a single message for the rest
 * (for example goalId, which has no input), joined with the non-field message.
 */
export function splitFieldErrors(
  error: ApiError,
  formFields: readonly string[]
): { fieldErrors: FieldErrors; message: string } {
  const fieldErrors: FieldErrors = {};
  const other: string[] = error.message ? [error.message] : [];

  for (const [field, message] of Object.entries(error.fieldErrors)) {
    if (formFields.includes(field)) {
      fieldErrors[field] = message;
    } else {
      other.push(message);
    }
  }

  return { fieldErrors, message: other.join(' ') };
}
