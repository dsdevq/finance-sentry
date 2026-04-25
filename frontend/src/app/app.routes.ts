import {type Routes} from '@angular/router';

import {authGuard} from './modules/auth/guards/auth.guard';
import {guestGuard} from './modules/auth/guards/guest.guard';
import {AppRoute} from './shared/enums/app-route/app-route.enum';

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
    path: '',
    loadComponent: () => import('./core/shell/app-shell.component').then(m => m.AppShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: AppRoute.Dashboard.slice(1),
        loadComponent: () =>
          import('./modules/bank-sync/pages/dashboard/dashboard.component').then(
            m => m.DashboardComponent
          ),
      },
      {
        path: AppRoute.Accounts.slice(1),
        loadChildren: () =>
          import('./modules/bank-sync/bank-sync.routes').then(
            ({BANK_SYNC_ROUTES}) => BANK_SYNC_ROUTES
          ),
      },
      {
        path: AppRoute.Transactions.slice(1),
        loadComponent: () =>
          import('./modules/bank-sync/pages/transaction-ledger/transaction-ledger.component').then(
            m => m.TransactionLedgerComponent
          ),
      },
      {
        path: AppRoute.Holdings.slice(1),
        loadComponent: () =>
          import('./modules/holdings/pages/holdings/holdings.component').then(
            m => m.HoldingsComponent
          ),
      },
      {path: '', redirectTo: AppRoute.Accounts, pathMatch: 'full'},
    ],
  },
];
