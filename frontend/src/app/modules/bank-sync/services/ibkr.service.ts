import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {type Observable} from 'rxjs';

import {type ConnectIBKRRequest, type IBKRConnectResult} from '../models/ibkr/ibkr.model';

@Injectable({providedIn: 'root'})
export class IBKRService extends ApiService {
  constructor() {
    super('brokerage/ibkr');
  }

  public connect(payload: ConnectIBKRRequest): Observable<IBKRConnectResult> {
    return this.post<IBKRConnectResult>('connect', payload);
  }

  public disconnect(): Observable<void> {
    return this.delete<void>('disconnect');
  }
}
