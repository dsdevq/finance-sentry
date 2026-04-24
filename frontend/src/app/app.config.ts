import {provideHttpClient, withInterceptors} from '@angular/common/http';
import {type ApplicationConfig} from '@angular/core';
import {provideRouter} from '@angular/router';

import {APP_ROUTES} from './app.routes';
import {provideErrorHandler} from './core/providers/error-handler.provider';
import {provideErrorMessages} from './core/providers/error-messages.provider';
import {authInterceptor} from './modules/auth/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(APP_ROUTES),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideErrorHandler(),
    provideErrorMessages(),
  ],
};
