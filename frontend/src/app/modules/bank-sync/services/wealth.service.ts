import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {type Observable} from 'rxjs';

import {environment} from '../../../../environments/environment';
import {type WealthSummaryResponse} from '../models/wealth.model';

@Injectable({providedIn: 'root'})
export class WealthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/wealth`;

  public getSummary(): Observable<WealthSummaryResponse> {
    return this.http.get<WealthSummaryResponse>(`${this.baseUrl}/summary`);
  }
}
