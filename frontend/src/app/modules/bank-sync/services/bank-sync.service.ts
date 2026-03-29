import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, timer } from 'rxjs';
import { shareReplay, switchMap, takeWhile } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import {
  AccountsResponse,
  ConnectResponse,
  LinkAccountResponse,
} from '../models/bank-account.model';
import {
  TransactionListResponse,
  TransactionQueryParams,
} from '../models/transaction.model';

export interface MonthlyFlow {
  month: string;
  currency: string;
  inflow: number;
  outflow: number;
  net: number;
}

export interface CategoryStat {
  category: string;
  totalSpend: number;
  percentOfTotal: number;
}

export interface DashboardData {
  aggregatedBalance: Record<string, number>;
  accountCount: number;
  accountsByType: Record<string, number>;
  monthlyFlow: MonthlyFlow[];
  topCategories: CategoryStat[];
  lastSyncTimestamp: string | null;
}

export interface SyncStatusResponse {
  status: 'pending' | 'running' | 'success' | 'failed';
  transactionCountFetched: number;
  transactionCountDeduped: number;
  errorMessage: string | null;
  lastSyncTimestamp: string | null;
  startedAt: string | null;
  completedAt: string | null;
}

export interface TriggerSyncResponse {
  jobId: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class BankSyncService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/accounts`;

  getLinkToken(): Observable<ConnectResponse> {
    return this.http.post<ConnectResponse>(`${this.baseUrl}/connect`, {});
  }

  exchangePublicToken(publicToken: string): Observable<LinkAccountResponse> {
    return this.http.post<LinkAccountResponse>(`${this.baseUrl}/link`, {
      publicToken,
    });
  }

  getAccounts(status?: string, currency?: string): Observable<AccountsResponse> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    if (currency) params = params.set('currency', currency);
    return this.http.get<AccountsResponse>(this.baseUrl, { params });
  }

  getTransactions(
    accountId: string,
    queryParams?: TransactionQueryParams,
  ): Observable<TransactionListResponse> {
    let params = new HttpParams();
    if (queryParams?.startDate) params = params.set('start_date', queryParams.startDate);
    if (queryParams?.endDate) params = params.set('end_date', queryParams.endDate);
    if (queryParams?.offset !== undefined)
      params = params.set('offset', queryParams.offset.toString());
    if (queryParams?.limit !== undefined)
      params = params.set('limit', queryParams.limit.toString());
    if (queryParams?.status) params = params.set('status', queryParams.status);
    if (queryParams?.sort) params = params.set('sort', queryParams.sort);
    return this.http.get<TransactionListResponse>(
      `${this.baseUrl}/${accountId}/transactions`,
      { params },
    );
  }

  triggerSync(accountId: string): Observable<TriggerSyncResponse> {
    return this.http.post<TriggerSyncResponse>(`${this.baseUrl}/${accountId}/sync`, {});
  }

  getSyncStatus(accountId: string): Observable<SyncStatusResponse> {
    return this.http.get<SyncStatusResponse>(`${this.baseUrl}/${accountId}/sync-status`);
  }

  pollSyncStatus(accountId: string, intervalMs = 2000): Observable<SyncStatusResponse> {
    return timer(0, intervalMs).pipe(
      switchMap(() => this.getSyncStatus(accountId)),
      takeWhile(s => s.status !== 'success' && s.status !== 'failed', true),
      shareReplay(1),
    );
  }

  disconnectAccount(accountId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${accountId}`);
  }

  getDashboardData(): Observable<DashboardData> {
    return this.http.get<DashboardData>(`${environment.apiBaseUrl}/api/dashboard/aggregated`);
  }
}
