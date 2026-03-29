import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AccountsListComponent } from './pages/accounts-list/accounts-list.component';
import { ConnectAccountComponent } from './pages/connect-account/connect-account.component';
import { TransactionListComponent } from './pages/transaction-list/transaction-list.component';
const routes: Routes = [
  { path: '', redirectTo: 'list', pathMatch: 'full' },
  { path: 'list', component: AccountsListComponent },
  { path: 'connect', component: ConnectAccountComponent },
  { path: ':accountId/transactions', component: TransactionListComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class BankSyncRoutingModule {}
