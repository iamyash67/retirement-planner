import { HttpErrorResponse } from '@angular/common/http';
import { parseApiError, splitFieldErrors } from './api-errors';

describe('parseApiError', () => {
  const fallback = 'Something went wrong.';

  it('maps a 400 ValidationProblemDetails to the first message per field', () => {
    const err = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: {
          retirementAge: ['Retirement age must be greater than current age', 'second'],
          currentSavings: ['You have enough savings to reach your goal'],
          empty: [],
        },
      },
    });

    expect(parseApiError(err, fallback)).toEqual({
      message: '',
      fieldErrors: {
        retirementAge: 'Retirement age must be greater than current age',
        currentSavings: 'You have enough savings to reach your goal',
      },
    });
  });

  it('keeps plain-string bodies from 404 and 409 as the message', () => {
    expect(parseApiError(new HttpErrorResponse({ status: 409, error: 'Investment already recorded' }), fallback))
      .toEqual({ message: 'Investment already recorded', fieldErrors: {} });
    expect(parseApiError(new HttpErrorResponse({ status: 404, error: 'Goal not found' }), fallback))
      .toEqual({ message: 'Goal not found', fieldErrors: {} });
  });

  it('falls back for a 500 ProblemDetails or a network error', () => {
    expect(parseApiError(new HttpErrorResponse({ status: 500, error: { title: 'An unexpected error occurred.' } }), fallback))
      .toEqual({ message: fallback, fieldErrors: {} });
    expect(parseApiError(new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') }), fallback))
      .toEqual({ message: fallback, fieldErrors: {} });
  });
});

describe('splitFieldErrors', () => {
  it('keeps errors for form fields and joins the rest into the message', () => {
    const result = splitFieldErrors(
      { message: '', fieldErrors: { month: 'Month must be between 1-12', goalId: 'Invalid Goal ID' } },
      ['year', 'month', 'monthlyInvestment']
    );

    expect(result).toEqual({ fieldErrors: { month: 'Month must be between 1-12' }, message: 'Invalid Goal ID' });
  });

  it('passes a plain message through', () => {
    expect(splitFieldErrors({ message: 'Goal not found', fieldErrors: {} }, ['year']))
      .toEqual({ fieldErrors: {}, message: 'Goal not found' });
  });
});
