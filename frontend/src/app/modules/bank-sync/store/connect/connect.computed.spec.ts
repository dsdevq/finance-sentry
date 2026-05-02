import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {ERROR_MESSAGES} from '@dsdevq-common/core';
import {beforeEach, describe, expect, it} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../../core/errors/error-messages.registry';
import {type Provider} from '../../../../shared/models/provider/provider.model';
import {type ModalStep} from '../../models/connect/connect.model';
import {connectComputed} from './connect.computed';
import {type ConnectStatus} from './connect.state';

function build(
  overrides: Partial<{
    selectedProvider: Provider;
    status: ConnectStatus;
    errorCode: Nullable<string>;
    modalStep: ModalStep;
  }> = {}
) {
  return {
    selectedProvider: signal<Provider>(overrides.selectedProvider ?? 'plaid'),
    status: signal<ConnectStatus>(overrides.status ?? 'idle'),
    errorCode: signal<Nullable<string>>(overrides.errorCode ?? null),
    statusMessage: signal<Nullable<string>>(null),
    modalStep: signal<ModalStep>(overrides.modalStep ?? 'closed'),
  };
}

describe('connectComputed', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY}],
    });
  });

  it('isBusy covers initializing/syncing/polling', () => {
    for (const s of ['initializing', 'syncing', 'polling'] as const) {
      const store = build({status: s});
      TestBed.runInInjectionContext(() => {
        expect(connectComputed(store).isBusy()).toBe(true);
      });
    }
  });

  it('isBusy is false when idle/ready/error/success', () => {
    for (const s of ['idle', 'ready', 'error', 'success'] as const) {
      const store = build({status: s});
      TestBed.runInInjectionContext(() => {
        expect(connectComputed(store).isBusy()).toBe(false);
      });
    }
  });

  it('errorMessage is empty when status is not error', () => {
    const store = build({status: 'idle', errorCode: 'ANY'});
    TestBed.runInInjectionContext(() => {
      expect(connectComputed(store).errorMessage()).toBe('');
    });
  });

  it('errorMessage resolves known monobank code via registry', () => {
    const store = build({
      status: 'error',
      errorCode: 'MONOBANK_TOKEN_DUPLICATE',
      selectedProvider: 'monobank',
    });
    TestBed.runInInjectionContext(() => {
      expect(connectComputed(store).errorMessage()).toContain('already connected');
    });
  });

  it('errorMessage falls back to monobank default when code is unknown', () => {
    const store = build({status: 'error', errorCode: 'UNKNOWN', selectedProvider: 'monobank'});
    TestBed.runInInjectionContext(() => {
      expect(connectComputed(store).errorMessage()).toContain('Failed to connect Monobank');
    });
  });

  it('errorMessage falls back to plaid init default when plaid with unknown code', () => {
    const store = build({status: 'error', errorCode: 'UNKNOWN', selectedProvider: 'plaid'});
    TestBed.runInInjectionContext(() => {
      expect(connectComputed(store).errorMessage()).toContain('Failed to initialize');
    });
  });

  it('errorMessage maps PLAID_LINK_FAILED to link-specific copy', () => {
    const store = build({
      status: 'error',
      errorCode: 'PLAID_LINK_FAILED',
      selectedProvider: 'plaid',
    });
    TestBed.runInInjectionContext(() => {
      expect(connectComputed(store).errorMessage()).toContain('Failed to link');
    });
  });
});
