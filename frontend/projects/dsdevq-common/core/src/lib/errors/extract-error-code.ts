import {HttpErrorResponse} from '@angular/common/http';

export function extractErrorCode(err: unknown): string | null {
  if (err instanceof HttpErrorResponse && err.error && typeof err.error === 'object') {
    const code = (err.error as {errorCode?: unknown}).errorCode;
    if (typeof code === 'string') {
      return code;
    }
  }
  return null;
}
