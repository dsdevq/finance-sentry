import {Routes} from '@angular/router';
import {AccountsListComponent} from './pages/accounts-list/accounts-list.component';
import {ConnectAccountComponent} from './pages/connect-account/connect-account.component';
import {TransactionListComponent} from './pages/transaction-list/transaction-list.component';
export const BANK_SYNC_ROUTES: Routes = [
  {path: '', redirectTo: 'list', pathMatch: 'full'},
  {path: 'list', component: AccountsListComponent},
  {path: 'connect', component: ConnectAccountComponent},
  {path: ':accountId/transactions', component: TransactionListComponent},
];
