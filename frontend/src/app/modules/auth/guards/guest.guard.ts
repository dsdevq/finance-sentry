import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';

import {AppRoute} from '../../../shared/enums/app-route.enum';
import {AuthService} from '../services/auth.service';

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return router.createUrlTree([AppRoute.Accounts]);
  }

  return true;
};
