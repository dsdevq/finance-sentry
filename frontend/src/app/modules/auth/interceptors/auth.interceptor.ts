import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
  HttpStatusCode,
} from '@angular/common/http';
import {inject} from '@angular/core';
import {catchError, switchMap, throwError} from 'rxjs';

import {AUTHORIZATION_HEADER} from '../constants/auth.constants';
import {AuthService} from '../services/auth.service';
import {AuthStore} from '../store/auth.store';

function isRefreshOrAuthRequest(req: HttpRequest<unknown>): boolean {
  return req.url.includes('/auth/refresh') || req.url.includes('/auth/logout');
}

function attachToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? req.clone({setHeaders: {[AUTHORIZATION_HEADER]: `Bearer ${token}`}}) : req;
}

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const authStore = inject(AuthStore);
  const authService = inject(AuthService);
  const authReq = attachToken(req, authStore.token());

  return next(authReq).pipe(
    catchError((err: unknown) => {
      const isUnauthorized =
        err instanceof HttpErrorResponse &&
        (err.status as HttpStatusCode) === HttpStatusCode.Unauthorized;

      if (isUnauthorized && !isRefreshOrAuthRequest(req)) {
        return authService.refresh().pipe(
          switchMap(refreshed => {
            authStore.applyAuthResponse(refreshed);
            return next(attachToken(req, refreshed.token));
          }),
          catchError(() => {
            authStore.logout();
            return throwError(() => err);
          })
        );
      }

      if (isUnauthorized && isRefreshOrAuthRequest(req)) {
        authStore.logout();
      }

      return throwError(() => err);
    })
  );
};
