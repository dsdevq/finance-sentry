import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {type Observable} from 'rxjs';

import {type WealthSummaryResponse} from '../../../shared/models/wealth/wealth.model';

@Injectable({providedIn: 'root'})
export class HoldingsService extends ApiService {
  constructor() {
    super('wealth');
  }

  public getSummary(): Observable<WealthSummaryResponse> {
    return this.get<WealthSummaryResponse>('summary');
  }
}
