import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {type Observable} from 'rxjs';

import {
  type ConnectBinanceRequest,
  type ConnectBinanceResponse,
} from '../models/binance/binance.model';

@Injectable({providedIn: 'root'})
export class BinanceService extends ApiService {
  constructor() {
    super('crypto/binance');
  }

  public connect(request: ConnectBinanceRequest): Observable<ConnectBinanceResponse> {
    return this.post<ConnectBinanceResponse>('connect', request);
  }

  public disconnect(): Observable<void> {
    return this.delete<void>();
  }
}
