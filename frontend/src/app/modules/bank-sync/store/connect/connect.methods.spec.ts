import {signalState} from '@ngrx/signals';
import {describe, expect, it} from 'vitest';

import {connectMethods} from './connect.methods';
import {initialConnectState} from './connect.state';

describe('connectMethods', () => {
  it('selectProvider updates provider and clears error/status', () => {
    const state = signalState(initialConnectState);
    const methods = connectMethods(state);
    methods.setError('X');

    methods.selectProvider('monobank');

    expect(state.selectedProvider()).toBe('monobank');
    expect(state.errorCode()).toBeNull();
    expect(state.statusMessage()).toBeNull();
  });

  it('setInitializing clears error and statusMessage', () => {
    const state = signalState(initialConnectState);
    const methods = connectMethods(state);
    methods.setError('X');
    methods.setSyncing('old');

    methods.setInitializing();

    expect(state.status()).toBe('initializing');
    expect(state.errorCode()).toBeNull();
    expect(state.statusMessage()).toBeNull();
  });

  it('setReady transitions status and clears error', () => {
    const state = signalState(initialConnectState);
    const methods = connectMethods(state);
    methods.setError('X');

    methods.setReady();

    expect(state.status()).toBe('ready');
    expect(state.errorCode()).toBeNull();
  });

  it('setSyncing carries the message and clears errors', () => {
    const state = signalState(initialConnectState);
    const methods = connectMethods(state);

    methods.setSyncing('wait');

    expect(state.status()).toBe('syncing');
    expect(state.statusMessage()).toBe('wait');
    expect(state.errorCode()).toBeNull();
  });

  it('setPolling updates status and message', () => {
    const state = signalState(initialConnectState);
    const methods = connectMethods(state);

    methods.setPolling('polling');

    expect(state.status()).toBe('polling');
    expect(state.statusMessage()).toBe('polling');
  });

  it('setSuccess resets transient fields', () => {
    const state = signalState(initialConnectState);
    const methods = connectMethods(state);
    methods.setSyncing('wait');

    methods.setSuccess();

    expect(state.status()).toBe('success');
    expect(state.statusMessage()).toBeNull();
    expect(state.errorCode()).toBeNull();
  });

  it('setError stores the code', () => {
    const state = signalState(initialConnectState);
    const methods = connectMethods(state);

    methods.setError('BOOM');

    expect(state.status()).toBe('error');
    expect(state.errorCode()).toBe('BOOM');
  });
});
