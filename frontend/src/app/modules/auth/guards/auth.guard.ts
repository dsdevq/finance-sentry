import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';

import {AppRoute} from '../../../shared/enums/app-route.enum';
import {AuthService} from '../services/auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree([AppRoute.Login], {
    queryParams: {returnUrl: state.url},
  });
};
