import {
  type EnvironmentProviders,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import {catchError, EMPTY, firstValueFrom, tap} from 'rxjs';

import {AuthService} from '../../modules/auth/services/auth.service';
import {AuthStore} from '../../modules/auth/store/auth.store';

export function provideAppInit(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => {
      const authService = inject(AuthService);
      const authStore = inject(AuthStore);
      return firstValueFrom(
        authService.getMe().pipe(
          tap(res => authStore.applyAuthResponse(res)),
          catchError(() => EMPTY)
        ),
        {defaultValue: undefined}
      );
    }),
  ]);
}
