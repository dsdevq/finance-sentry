import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {type Observable} from 'rxjs';

import {environment} from '../../../../environments/environment';
import {
  type ConnectBinanceRequest,
  type ConnectBinanceResponse,
} from '../models/binance/binance.model';

@Injectable({providedIn: 'root'})
export class BinanceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/crypto/binance`;

  public connect(request: ConnectBinanceRequest): Observable<ConnectBinanceResponse> {
    return this.http.post<ConnectBinanceResponse>(`${this.baseUrl}/connect`, request);
  }

  public disconnect(): Observable<void> {
    return this.http.delete<void>(this.baseUrl);
  }
}
