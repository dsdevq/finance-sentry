import {HttpErrorResponse} from '@angular/common/http';
import {type ErrorHandler, inject, Injectable} from '@angular/core';
import {ToastService} from '@dsdevq-common/ui';

const UNAUTHORIZED_STATUS = 401;

@Injectable()
export class HttpErrorHandler implements ErrorHandler {
  private readonly toastService = inject(ToastService);

  public handleError(error: unknown): void {
    if (error instanceof HttpErrorResponse) {
      if (error.status === UNAUTHORIZED_STATUS) {
        return;
      }
      const message =
        (error.error as {message?: string} | null)?.message ?? 'An unexpected error occurred.';
      this.toastService.error(message);
    }
    console.error(error);
  }
}
