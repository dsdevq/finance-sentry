import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BankSyncRoutingModule } from './bank-sync-routing.module';
import { AccountsListComponent } from './pages/accounts-list/accounts-list.component';
import { ConnectAccountComponent } from './pages/connect-account/connect-account.component';
import { TransactionListComponent } from './pages/transaction-list/transaction-list.component';

@NgModule({
  imports: [
    CommonModule,
    BankSyncRoutingModule,
    AccountsListComponent,
    ConnectAccountComponent,
    TransactionListComponent,
  ],
})
export class BankSyncModule {}
