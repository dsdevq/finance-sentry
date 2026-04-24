import {signalState} from '@ngrx/signals';
import {describe, expect, it} from 'vitest';

import {type SyncStatusResponse} from '../../models/sync.model';
import {syncStatusMethods} from './sync-status.methods';
import {initialSyncStatusState} from './sync-status.state';

function mkStatus(overrides: Partial<SyncStatusResponse> = {}): SyncStatusResponse {
  return {
    status: 'success',
    transactionCountFetched: 0,
    transactionCountDeduped: 0,
    errorMessage: null,
    lastSyncTimestamp: null,
    startedAt: null,
    completedAt: null,
    ...overrides,
  };
}

describe('syncStatusMethods', () => {
  it('setStatus running/pending flips isSyncing on', () => {
    const state = signalState(initialSyncStatusState);
    const methods = syncStatusMethods(state);

    methods.setStatus(mkStatus({status: 'running'}));

    expect(state.isSyncing()).toBe(true);
    expect(state.errorMessage()).toBeNull();
  });

  it('setStatus success clears isSyncing', () => {
    const state = signalState(initialSyncStatusState);
    const methods = syncStatusMethods(state);
    methods.markTriggering();

    methods.setStatus(mkStatus({status: 'success'}));

    expect(state.isSyncing()).toBe(false);
  });

  it('setStatus failed carries errorMessage or falls back', () => {
    const state = signalState(initialSyncStatusState);
    const methods = syncStatusMethods(state);

    methods.setStatus(mkStatus({status: 'failed', errorMessage: 'upstream down'}));
    expect(state.errorMessage()).toBe('upstream down');

    methods.setStatus(mkStatus({status: 'failed'}));
    expect(state.errorMessage()).toContain('Sync failed');
  });

  it('markTriggering sets syncing + clears error', () => {
    const state = signalState(initialSyncStatusState);
    const methods = syncStatusMethods(state);
    methods.markTriggerFailed();

    methods.markTriggering();

    expect(state.isSyncing()).toBe(true);
    expect(state.errorMessage()).toBeNull();
  });

  it('markTriggerFailed / markPollFailed set error and stop syncing', () => {
    const state = signalState(initialSyncStatusState);
    const methods = syncStatusMethods(state);
    methods.markTriggering();

    methods.markTriggerFailed();
    expect(state.isSyncing()).toBe(false);
    expect(state.errorMessage()).toContain('Failed to trigger');

    methods.markTriggering();
    methods.markPollFailed();
    expect(state.isSyncing()).toBe(false);
    expect(state.errorMessage()).toContain('Sync status check failed');
  });
});
