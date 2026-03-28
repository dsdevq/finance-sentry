import { Routes } from '@angular/router';

export const APP_ROUTES: Routes = [
  {
    path: 'accounts',
    loadChildren: () =>
      import('./modules/bank-sync/bank-sync.module').then(
        (m) => m.BankSyncModule,
      ),
  },
  { path: '', redirectTo: '/accounts', pathMatch: 'full' },
];
