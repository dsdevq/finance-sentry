import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
  HttpStatusCode,
} from '@angular/common/http';
import {inject} from '@angular/core';
import {tap} from 'rxjs/operators';

import {AuthService} from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  const authReq = token ? req.clone({setHeaders: {['Authorization']: `Bearer ${token}`}}) : req;

  return next(authReq).pipe(
    tap({
      error: err => {
        if (err instanceof HttpErrorResponse && err.status === HttpStatusCode.Unauthorized) {
          authService.logout();
        }
      },
    })
  );
};
