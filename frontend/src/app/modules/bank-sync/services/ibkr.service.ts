import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {type Observable} from 'rxjs';

import {type ConnectIBKRResponse} from '../models/ibkr/ibkr.model';

@Injectable({providedIn: 'root'})
export class IBKRService extends ApiService {
  constructor() {
    super('brokerage/ibkr');
  }

  public connect(): Observable<ConnectIBKRResponse> {
    return this.post<ConnectIBKRResponse>('connect');
  }

  public disconnect(): Observable<void> {
    return this.delete<void>();
  }
}
