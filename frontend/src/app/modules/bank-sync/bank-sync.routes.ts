import {type Routes} from '@angular/router';

import {BankSyncRoute} from './enums/bank-sync-route/bank-sync-route.enum';

export const BANK_SYNC_ROUTES: Routes = [
  {path: '', redirectTo: BankSyncRoute.List, pathMatch: 'full'},
  {
    path: BankSyncRoute.List,
    loadComponent: () =>
      import('./pages/accounts-list/accounts-list.component').then(m => m.AccountsListComponent),
  },
  {
    path: ':accountId/transactions',
    loadComponent: () =>
      import('./pages/transaction-list/transaction-list.component').then(
        m => m.TransactionListComponent
      ),
  },
];
