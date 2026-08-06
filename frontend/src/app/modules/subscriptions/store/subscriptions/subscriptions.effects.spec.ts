import {TestBed} from '@angular/core/testing';
import {of} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {
  type Subscription,
  type SubscriptionsListResponse,
  type SubscriptionSummary,
} from '../../models/subscription/subscription.model';
import {SubscriptionsService} from '../../services/subscriptions.service';
import {subscriptionsEffects} from './subscriptions.effects';

const SUBSCRIPTION: Subscription = {
  id: 'sub-1',
  merchantName: 'Netflix',
  cadence: 'monthly',
  averageAmount: 10,
  lastKnownAmount: 10,
  monthlyEquivalent: 10,
  currency: 'EUR',
  lastChargeDate: '2026-08-01',
  nextExpectedDate: '2026-09-01',
  status: 'active',
  occurrenceCount: 3,
  kind: 'subscription',
  termCount: null,
  remainingPayments: null,
  isManual: false,
};

const LIST_RESPONSE: SubscriptionsListResponse = {
  items: [SUBSCRIPTION],
  totalCount: 1,
  hasInsufficientHistory: false,
};

const SUMMARY: SubscriptionSummary = {
  totalMonthlyEstimate: 10,
  totalAnnualEstimate: 120,
  activeCount: 1,
  potentiallyCancelledCount: 0,
  currency: 'EUR',
};

function buildStore() {
  return {
    setData: vi.fn(),
    setSummary: vi.fn(),
    dismissSubscription: vi.fn(),
    restoreSubscription: vi.fn(),
  };
}

function buildService() {
  return {
    getSubscriptions: vi.fn(),
    getSummary: vi.fn(),
    dismiss: vi.fn(),
    restore: vi.fn(),
  };
}

function configure(service: ReturnType<typeof buildService>) {
  TestBed.configureTestingModule({
    providers: [{provide: SubscriptionsService, useValue: service}],
  });
}

describe('subscriptionsEffects', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('load: stores list and summary', () => {
    const store = buildStore();
    const service = buildService();
    service.getSubscriptions.mockReturnValue(of(LIST_RESPONSE));
    service.getSummary.mockReturnValue(of(SUMMARY));
    configure(service);

    TestBed.runInInjectionContext(() => subscriptionsEffects(store).load());

    expect(service.getSubscriptions).toHaveBeenCalledWith(true);
    expect(store.setData).toHaveBeenCalledWith([SUBSCRIPTION], false);
    expect(store.setSummary).toHaveBeenCalledWith(SUMMARY);
  });

  it('dismiss: updates the list row and refetches the summary', () => {
    const store = buildStore();
    const service = buildService();
    service.dismiss.mockReturnValue(of(void 0));
    service.getSummary.mockReturnValue(of(SUMMARY));
    configure(service);

    TestBed.runInInjectionContext(() => subscriptionsEffects(store).dismiss('sub-1'));

    expect(service.dismiss).toHaveBeenCalledWith('sub-1');
    expect(store.dismissSubscription).toHaveBeenCalledWith('sub-1');
    expect(service.getSummary).toHaveBeenCalled();
    expect(store.setSummary).toHaveBeenCalledWith(SUMMARY);
  });

  it('restore: updates the list row and refetches the summary', () => {
    const store = buildStore();
    const service = buildService();
    service.restore.mockReturnValue(of(void 0));
    service.getSummary.mockReturnValue(of(SUMMARY));
    configure(service);

    TestBed.runInInjectionContext(() => subscriptionsEffects(store).restore('sub-1'));

    expect(service.restore).toHaveBeenCalledWith('sub-1');
    expect(store.restoreSubscription).toHaveBeenCalledWith('sub-1');
    expect(service.getSummary).toHaveBeenCalled();
    expect(store.setSummary).toHaveBeenCalledWith(SUMMARY);
  });
});
