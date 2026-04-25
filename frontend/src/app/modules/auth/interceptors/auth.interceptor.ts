import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
  HttpStatusCode,
} from '@angular/common/http';
import {inject} from '@angular/core';
import {catchError, switchMap, throwError} from 'rxjs';

import {AuthService} from '../services/auth.service';
import {AuthStore} from '../store/auth.store';

function isAuthEndpoint(req: HttpRequest<unknown>): boolean {
  return req.url.includes('/auth/');
}

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const authStore = inject(AuthStore);
  const authService = inject(AuthService);
  const authReq = req.clone({withCredentials: true});

  return next(authReq).pipe(
    catchError((err: unknown) => {
      const isUnauthorized =
        err instanceof HttpErrorResponse &&
        (err.status as HttpStatusCode) === HttpStatusCode.Unauthorized;

      if (isUnauthorized && !isAuthEndpoint(req)) {
        return authService.refresh().pipe(
          switchMap(res => {
            authStore.applyAuthResponse(res);
            return next(authReq);
          }),
          catchError(() => {
            authStore.logout();
            return throwError(() => err);
          })
        );
      }

      if (isUnauthorized && isAuthEndpoint(req)) {
        authStore.logout();
      }

      return throwError(() => err);
    })
  );
};
