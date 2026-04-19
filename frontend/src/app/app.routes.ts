import {type Routes} from '@angular/router';

import {authGuard} from './modules/auth/guards/auth.guard';
import {guestGuard} from './modules/auth/guards/guest.guard';
import {AppRoute} from './shared/enums/app-route.enum';

export const APP_ROUTES: Routes = [
  {
    path: AppRoute.Login.slice(1),
    loadComponent: () =>
      import('./modules/auth/pages/login/login.component').then(m => m.LoginComponent),
    canActivate: [guestGuard],
  },
  {
    path: AppRoute.Register.slice(1),
    loadComponent: () =>
      import('./modules/auth/pages/register/register.component').then(m => m.RegisterComponent),
    canActivate: [guestGuard],
  },
  {
    path: AppRoute.Accounts.slice(1),
    loadChildren: () =>
      import('./modules/bank-sync/bank-sync.routes').then(({BANK_SYNC_ROUTES}) => BANK_SYNC_ROUTES),
    canActivate: [authGuard],
  },
  {
    path: AppRoute.Dashboard.slice(1),
    loadComponent: () =>
      import('./modules/bank-sync/pages/dashboard/dashboard.component').then(
        m => m.DashboardComponent
      ),
    canActivate: [authGuard],
  },
  {path: '', redirectTo: AppRoute.Accounts, pathMatch: 'full'},
];
