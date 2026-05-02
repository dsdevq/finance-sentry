import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {ERROR_MESSAGES} from '@dsdevq-common/ui';
import {beforeEach, describe, expect, it} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../../core/errors/error-messages.registry';
import {transactionsComputed} from './transactions.computed';
import {PAGE_SIZE} from './transactions.state';

function build(
  overrides: Partial<{
    status: AsyncStatus;
    errorCode: Nullable<string>;
    offset: number;
    totalCount: number;
  }> = {}
) {
  return {
    status: signal<AsyncStatus>(overrides.status ?? 'idle'),
    errorCode: signal<Nullable<string>>(overrides.errorCode ?? null),
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

  it('pagination mirrors offset/totalCount with PAGE_SIZE limit', () => {
    const store = build({offset: PAGE_SIZE, totalCount: PAGE_SIZE * 3});
    TestBed.runInInjectionContext(() => {
      const p = transactionsComputed(store).pagination();
      expect(p.offset).toBe(PAGE_SIZE);
      expect(p.limit).toBe(PAGE_SIZE);
      expect(p.totalCount).toBe(PAGE_SIZE * 3);
      expect(p.hasMore).toBe(true);
    });
  });

  it('pagination.hasMore is false on last page', () => {
    const store = build({offset: PAGE_SIZE * 2, totalCount: PAGE_SIZE * 3});
    TestBed.runInInjectionContext(() => {
      expect(transactionsComputed(store).pagination().hasMore).toBe(false);
    });
  });
});
