import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {Observable, timer} from 'rxjs';
import {shareReplay, switchMap, takeWhile} from 'rxjs/operators';

import {environment} from '../../../../environments/environment';
import {
  AccountsResponse,
  ConnectMonobankResponse,
  ConnectResponse,
  LinkAccountResponse,
} from '../models/bank-account/bank-account.model';
import {DashboardData} from '../models/dashboard/dashboard.model';
import {SyncStatusResponse, TriggerSyncResponse} from '../models/sync/sync.model';
import {
  type GetAllTransactionsParams,
  type GlobalTransactionsResponse,
  type TransactionListResponse,
  type TransactionQueryParams,
} from '../models/transaction/transaction.model';

export type {DashboardData, SyncStatusResponse, TriggerSyncResponse};

const DEFAULT_SYNC_POLL_INTERVAL_MS = 2000;

@Injectable({providedIn: 'root'})
export class BankSyncService extends ApiService {
  constructor() {
    super('accounts');
  }

  public connectMonobank(token: string): Observable<ConnectMonobankResponse> {
    return this.post<ConnectMonobankResponse>('monobank/connect', {token});
  }

  public getLinkToken(): Observable<ConnectResponse> {
    return this.post<ConnectResponse>('connect');
  }

  public exchangePublicToken(
    publicToken: string,
    institutionName: string
  ): Observable<LinkAccountResponse> {
    return this.post<LinkAccountResponse>('link', {publicToken, institutionName});
  }

  public getAccounts(status?: string, currency?: string): Observable<AccountsResponse> {
    return this.get<AccountsResponse>('', {status, currency});
  }

  public getTransactions(
    accountId: string,
    queryParams?: TransactionQueryParams
  ): Observable<TransactionListResponse> {
    return this.get<TransactionListResponse>(`${accountId}/transactions`, queryParams);
  }

  public getAllTransactions(
    params?: GetAllTransactionsParams
  ): Observable<GlobalTransactionsResponse> {
    return this.get<GlobalTransactionsResponse>('transactions', params);
  }

  public triggerSync(accountId: string): Observable<TriggerSyncResponse> {
    return this.post<TriggerSyncResponse>(`${accountId}/sync`);
  }

  public getSyncStatus(accountId: string): Observable<SyncStatusResponse> {
    return this.get<SyncStatusResponse>(`${accountId}/sync-status`);
  }

  public pollSyncStatus(
    accountId: string,
    intervalMs = DEFAULT_SYNC_POLL_INTERVAL_MS
  ): Observable<SyncStatusResponse> {
    return timer(0, intervalMs).pipe(
      switchMap(() => this.getSyncStatus(accountId)),
      takeWhile(s => s.status !== 'success' && s.status !== 'failed', true),
      shareReplay(1)
    );
  }

  public disconnectAccount(accountId: string): Observable<void> {
    return this.delete<void>(accountId);
  }

  public disconnectMonobank(): Observable<void> {
    return this.delete<void>('monobank');
  }

  public getDashboardData(): Observable<DashboardData> {
    return this.http.get<DashboardData>(`${environment.apiBaseUrl}/dashboard/aggregated`);
  }
}
