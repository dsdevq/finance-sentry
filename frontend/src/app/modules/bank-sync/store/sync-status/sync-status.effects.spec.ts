import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type SyncStatusResponse, type TriggerSyncResponse} from '../../models/sync.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {syncStatusEffects} from './sync-status.effects';

const SUCCESS_STATUS: SyncStatusResponse = {
  status: 'success',
  transactionCountFetched: 1,
  transactionCountDeduped: 0,
  errorMessage: null,
  lastSyncTimestamp: null,
  startedAt: null,
  completedAt: null,
};

const TRIGGER_RESPONSE: TriggerSyncResponse = {jobId: 'j', message: 'ok'};

function buildStore(accountId = 'a1') {
  return {
    accountId: signal(accountId),
    setStatus: vi.fn(),
    markTriggering: vi.fn(),
    markTriggerFailed: vi.fn(),
    markPollFailed: vi.fn(),
  };
}

function buildService() {
  return {
    getSyncStatus: vi.fn(),
    triggerSync: vi.fn(),
    pollSyncStatus: vi.fn(),
  };
}

function configure(service: ReturnType<typeof buildService>) {
  TestBed.configureTestingModule({
    providers: [{provide: BankSyncService, useValue: service}],
  });
}

describe('syncStatusEffects', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loadStatus: stores status on success', () => {
    const store = buildStore();
    const service = buildService();
    service.getSyncStatus.mockReturnValue(of(SUCCESS_STATUS));
    configure(service);

    TestBed.runInInjectionContext(() => syncStatusEffects(store).loadStatus());

    expect(service.getSyncStatus).toHaveBeenCalledWith('a1');
    expect(store.setStatus).toHaveBeenCalledWith(SUCCESS_STATUS);
  });

  it('loadStatus: swallows errors silently', () => {
    const store = buildStore();
    const service = buildService();
    service.getSyncStatus.mockReturnValue(throwError(() => new Error('down')));
    configure(service);

    TestBed.runInInjectionContext(() => syncStatusEffects(store).loadStatus());

    expect(store.markPollFailed).not.toHaveBeenCalled();
  });

  it('triggerSync: marks triggering and starts polling on success', () => {
    const store = buildStore();
    const service = buildService();
    service.triggerSync.mockReturnValue(of(TRIGGER_RESPONSE));
    service.pollSyncStatus.mockReturnValue(of(SUCCESS_STATUS));
    configure(service);

    TestBed.runInInjectionContext(() => syncStatusEffects(store).triggerSync());

    expect(store.markTriggering).toHaveBeenCalled();
    expect(service.triggerSync).toHaveBeenCalledWith('a1');
    expect(service.pollSyncStatus).toHaveBeenCalledWith('a1');
    expect(store.setStatus).toHaveBeenCalledWith(SUCCESS_STATUS);
  });

  it('triggerSync: marks trigger failed on error', () => {
    const store = buildStore();
    const service = buildService();
    service.triggerSync.mockReturnValue(throwError(() => new Error('down')));
    configure(service);

    TestBed.runInInjectionContext(() => syncStatusEffects(store).triggerSync());

    expect(store.markTriggerFailed).toHaveBeenCalled();
    expect(service.pollSyncStatus).not.toHaveBeenCalled();
  });
});
