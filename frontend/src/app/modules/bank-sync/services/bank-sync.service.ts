import {HttpClient, HttpParams} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
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
  GlobalTransactionsResponse,
  TransactionListResponse,
  TransactionQueryParams,
} from '../models/transaction/transaction.model';

export type {DashboardData, SyncStatusResponse, TriggerSyncResponse};

@Injectable({providedIn: 'root'})
export class BankSyncService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/accounts`;

  public connectMonobank(token: string): Observable<ConnectMonobankResponse> {
    return this.http.post<ConnectMonobankResponse>(`${this.baseUrl}/monobank/connect`, {token});
  }

  public getLinkToken(): Observable<ConnectResponse> {
    return this.http.post<ConnectResponse>(`${this.baseUrl}/connect`, {});
  }

  public exchangePublicToken(
    publicToken: string,
    institutionName: string
  ): Observable<LinkAccountResponse> {
    return this.http.post<LinkAccountResponse>(`${this.baseUrl}/link`, {
      publicToken,
      institutionName,
    });
  }

  public getAccounts(status?: string, currency?: string): Observable<AccountsResponse> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    if (currency) {
      params = params.set('currency', currency);
    }
    return this.http.get<AccountsResponse>(this.baseUrl, {params});
  }

  public getTransactions(
    accountId: string,
    queryParams?: TransactionQueryParams
  ): Observable<TransactionListResponse> {
    let params = new HttpParams();
    if (queryParams?.startDate) {
      params = params.set('start_date', queryParams.startDate);
    }
    if (queryParams?.endDate) {
      params = params.set('end_date', queryParams.endDate);
    }
    if (queryParams?.offset !== undefined) {
      params = params.set('offset', queryParams.offset.toString());
    }
    if (queryParams?.limit !== undefined) {
      params = params.set('limit', queryParams.limit.toString());
    }
    if (queryParams?.status) {
      params = params.set('status', queryParams.status);
    }
    if (queryParams?.sort) {
      params = params.set('sort', queryParams.sort);
    }
    return this.http.get<TransactionListResponse>(`${this.baseUrl}/${accountId}/transactions`, {
      params,
    });
  }

  public getAllTransactions(params?: {
    offset?: number;
    limit?: number;
    from?: string;
    to?: string;
  }): Observable<GlobalTransactionsResponse> {
    let httpParams = new HttpParams();
    if (params?.offset !== undefined) {
      httpParams = httpParams.set('offset', params.offset.toString());
    }
    if (params?.limit !== undefined) {
      httpParams = httpParams.set('limit', params.limit.toString());
    }
    if (params?.from) {
      httpParams = httpParams.set('from', params.from);
    }
    if (params?.to) {
      httpParams = httpParams.set('to', params.to);
    }
    return this.http.get<GlobalTransactionsResponse>(`${this.baseUrl}/transactions`, {
      params: httpParams,
    });
  }

  public triggerSync(accountId: string): Observable<TriggerSyncResponse> {
    return this.http.post<TriggerSyncResponse>(`${this.baseUrl}/${accountId}/sync`, {});
  }

  public getSyncStatus(accountId: string): Observable<SyncStatusResponse> {
    return this.http.get<SyncStatusResponse>(`${this.baseUrl}/${accountId}/sync-status`);
  }

  public pollSyncStatus(accountId: string, intervalMs = 2000): Observable<SyncStatusResponse> {
    return timer(0, intervalMs).pipe(
      switchMap(() => this.getSyncStatus(accountId)),
      takeWhile(s => s.status !== 'success' && s.status !== 'failed', true),
      shareReplay(1)
    );
  }

  public disconnectAccount(accountId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${accountId}`);
  }

  public disconnectMonobank(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/monobank`);
  }

  public getDashboardData(): Observable<DashboardData> {
    return this.http.get<DashboardData>(`${environment.apiBaseUrl}/dashboard/aggregated`);
  }
}
