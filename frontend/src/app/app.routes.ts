import { type Routes } from '@angular/router';
import { authGuard } from './modules/auth/guards/auth.guard';
import { guestGuard } from './modules/auth/guards/guest.guard';

export const APP_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./modules/auth/pages/login/login.component').then(m => m.LoginComponent),
    canActivate: [guestGuard]
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./modules/auth/pages/register/register.component').then(m => m.RegisterComponent),
    canActivate: [guestGuard]
  },
  {
    path: 'accounts',
    loadChildren: () =>
      import('./modules/bank-sync/bank-sync.routes').then(({ BANK_SYNC_ROUTES }) => BANK_SYNC_ROUTES),
    canActivate: [authGuard]
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./modules/bank-sync/pages/dashboard/dashboard.component').then(
        m => m.DashboardComponent
      ),
    canActivate: [authGuard]
  },
  { path: '', redirectTo: '/accounts', pathMatch: 'full' }
];
