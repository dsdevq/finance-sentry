import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {type Observable} from 'rxjs';

import {
  type ConnectIBKRRequest,
  type ConnectIBKRSessionAccepted,
  type IBKRConnectSessionSnapshot,
} from '../models/ibkr/ibkr.model';

@Injectable({providedIn: 'root'})
export class IBKRService extends ApiService {
  constructor() {
    super('brokerage/ibkr');
  }

  public createConnectSession(payload: ConnectIBKRRequest): Observable<ConnectIBKRSessionAccepted> {
    return this.post<ConnectIBKRSessionAccepted>('connect', payload);
  }

  public getConnectStatus(sessionId: string): Observable<IBKRConnectSessionSnapshot> {
    return this.get<IBKRConnectSessionSnapshot>(`connect/${sessionId}`);
  }

  public cancelConnect(sessionId: string): Observable<void> {
    return this.delete<void>(`connect/${sessionId}`);
  }

  public disconnect(): Observable<void> {
    return this.delete<void>('disconnect');
  }
}
