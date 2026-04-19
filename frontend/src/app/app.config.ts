import {provideHttpClient, withInterceptors} from '@angular/common/http';
import {type ApplicationConfig, ErrorHandler} from '@angular/core';
import {provideRouter} from '@angular/router';

import {APP_ROUTES} from './app.routes';
import {HttpErrorHandler} from './core/handlers/http-error.handler';
import {authInterceptor} from './modules/auth/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(APP_ROUTES),
    provideHttpClient(withInterceptors([authInterceptor])),
    {provide: ErrorHandler, useClass: HttpErrorHandler},
  ],
};
