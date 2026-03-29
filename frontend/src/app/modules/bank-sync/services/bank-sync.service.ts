import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
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
}
