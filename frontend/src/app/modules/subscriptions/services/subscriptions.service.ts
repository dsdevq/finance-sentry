import {Injectable} from '@angular/core';
import {ApiService} from '@dsdevq-common/core';
import {type Observable} from 'rxjs';

import {
  type SubscriptionsListResponse,
  type SubscriptionSummary,
} from '../models/subscription/subscription.model';

@Injectable({providedIn: 'root'})
export class SubscriptionsService extends ApiService {
  constructor() {
    super('subscriptions');
  }

  public getSubscriptions(includeDismissed = false): Observable<SubscriptionsListResponse> {
    return this.get<SubscriptionsListResponse>(
      '',
      includeDismissed ? {includeDismissed: true} : undefined
    );
  }

  public getSummary(): Observable<SubscriptionSummary> {
    return this.get<SubscriptionSummary>('summary');
  }

  public dismiss(id: string): Observable<void> {
    return this.patch<void>(`${id}/dismiss`);
  }

  public restore(id: string): Observable<void> {
    return this.patch<void>(`${id}/restore`);
  }
}
