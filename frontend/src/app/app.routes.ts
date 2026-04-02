import {Routes} from '@angular/router';

export const APP_ROUTES: Routes = [
  {
    path: 'accounts',
    loadChildren: () =>
      import('./modules/bank-sync/bank-sync.routes').then(({BANK_SYNC_ROUTES}) => BANK_SYNC_ROUTES),
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./modules/bank-sync/pages/dashboard/dashboard.component').then(
        (m) => m.DashboardComponent
      ),
  },
  {path: '', redirectTo: '/accounts', pathMatch: 'full'},
];
