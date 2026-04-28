import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {type Observable} from 'rxjs';

import {environment} from '../../../../environments/environment';
import {type ConnectIBKRResponse} from '../models/ibkr/ibkr.model';

@Injectable({providedIn: 'root'})
export class IBKRService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/brokerage/ibkr`;

  public connect(): Observable<ConnectIBKRResponse> {
    return this.http.post<ConnectIBKRResponse>(`${this.baseUrl}/connect`, {});
  }

  public disconnect(): Observable<void> {
    return this.http.delete<void>(this.baseUrl);
  }
}
