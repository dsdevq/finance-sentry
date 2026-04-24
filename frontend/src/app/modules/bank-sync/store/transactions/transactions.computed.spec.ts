import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {ERROR_MESSAGES} from '@dsdevq-common/ui';
import {beforeEach, describe, expect, it} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../../core/errors/error-messages.registry';
import {transactionsComputed} from './transactions.computed';
import {PAGE_SIZE, type TransactionsStatus} from './transactions.state';

function build(
  overrides: Partial<{
    status: TransactionsStatus;
    errorCode: string | null;
    offset: number;
    totalCount: number;
  }> = {}
) {
  return {
    status: signal<TransactionsStatus>(overrides.status ?? 'idle'),
    errorCode: signal<string | null>(overrides.errorCode ?? null),
    offset: signal(overrides.offset ?? 0),
    totalCount: signal(overrides.totalCount ?? 0),
  };
}

describe('transactionsComputed', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY}],
    });
  });

  it('isLoading reflects loading status', () => {
    const store = build({status: 'loading'});
    TestBed.runInInjectionContext(() => {
      expect(transactionsComputed(store).isLoading()).toBe(true);
    });
  });

  it('errorMessage is empty when not error', () => {
    const store = build({status: 'idle', errorCode: 'ANY'});
    TestBed.runInInjectionContext(() => {
      expect(transactionsComputed(store).errorMessage()).toBe('');
    });
  });

  it('errorMessage falls back to default for unknown code', () => {
    const store = build({status: 'error', errorCode: 'UNKNOWN'});
    TestBed.runInInjectionContext(() => {
      expect(transactionsComputed(store).errorMessage()).toContain('Failed to load transactions');
    });
  });

  it('currentPage derives from offset', () => {
    const store = build({offset: PAGE_SIZE * 2});
    TestBed.runInInjectionContext(() => {
      expect(transactionsComputed(store).currentPage()).toBe(3);
    });
  });

  it('totalPages is at least 1 for empty result', () => {
    const store = build({totalCount: 0});
    TestBed.runInInjectionContext(() => {
      expect(transactionsComputed(store).totalPages()).toBe(1);
    });
  });

  it('hasPrevious / hasNext reflect offset + totalCount', () => {
    const store = build({offset: PAGE_SIZE, totalCount: PAGE_SIZE * 3});
    TestBed.runInInjectionContext(() => {
      const c = transactionsComputed(store);
      expect(c.hasPrevious()).toBe(true);
      expect(c.hasNext()).toBe(true);
    });
  });

  it('hasNext is false on last page', () => {
    const store = build({offset: PAGE_SIZE * 2, totalCount: PAGE_SIZE * 3});
    TestBed.runInInjectionContext(() => {
      expect(transactionsComputed(store).hasNext()).toBe(false);
    });
  });
});
