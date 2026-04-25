import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, filter, map, pipe, race, switchMap, take, tap, timer} from 'rxjs';

import {ErrorUtils} from '../../../../shared/utils/error.utils';
import {type ConnectBinanceRequest} from '../../models/binance/binance.model';
import {type ModalStep} from '../../models/connect/connect.model';
import {type ConnectIBKRRequest} from '../../models/ibkr/ibkr.model';
import {type PlaidSuccessMetadata} from '../../models/plaid/plaid.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {BinanceService} from '../../services/binance.service';
import {IBKRService} from '../../services/ibkr.service';
import {PlaidLinkService} from '../../services/plaid-link.service';
import {AccountsStore} from '../accounts/accounts.store';

interface EffectsStore {
  setInitializing: () => void;
  setReady: () => void;
  setSyncing: (msg: string) => void;
  setPolling: (msg: string) => void;
  setSuccess: () => void;
  setError: (code: Nullable<string>) => void;
  modalStep: () => ModalStep;
  setModalStep: (step: ModalStep) => void;
}

const POLL_INTERVAL_MS = 3000;
const POLL_MAX_MS = 60_000;

interface PlaidSuccessPayload {
  publicToken: string;
  metadata: PlaidSuccessMetadata;
}

function resolveBackStep(step: ModalStep): ModalStep {
  switch (step) {
    case 'bank-picker':
      return 'type-picker';
    case 'monobank-form':
      return 'bank-picker';
    case 'binance-form':
      return 'type-picker';
    case 'ibkr-form':
      return 'type-picker';
    default:
      return 'closed';
  }
}

export function connectEffects(store: EffectsStore) {
  const bankSyncService = inject(BankSyncService);
  const binanceService = inject(BinanceService);
  const ibkrService = inject(IBKRService);
  const plaidService = inject(PlaidLinkService);
  const accountsStore = inject(AccountsStore, {optional: true});

  const pollForActive = rxMethod<void>(
    pipe(
      tap(() => store.setPolling('Syncing transaction history...')),
      switchMap(() => {
        const polling$ = timer(0, POLL_INTERVAL_MS).pipe(
          switchMap(() => bankSyncService.getAccounts()),
          filter(res => res.accounts.some(a => a.syncStatus === 'active')),
          take(1),
          map(() => 'active' as const)
        );
        const timeout$ = timer(POLL_MAX_MS).pipe(map(() => 'timeout' as const));
        return race(polling$, timeout$).pipe(
          tap(() => {
            store.setSuccess();
            accountsStore?.load();
          }),
          catchError((err: unknown) => {
            store.setError(ErrorUtils.extractCode(err));
            return EMPTY;
          })
        );
      })
    )
  );

  const exchangePlaidToken = rxMethod<PlaidSuccessPayload>(
    pipe(
      tap(() => store.setSyncing('Linking your account...')),
      switchMap(({publicToken, metadata}) => {
        const institutionName = metadata.institution?.name ?? 'Unknown';
        return bankSyncService.exchangePublicToken(publicToken, institutionName).pipe(
          tap(() => pollForActive()),
          catchError((err: unknown) => {
            store.setError(ErrorUtils.extractCode(err) ?? 'PLAID_LINK_FAILED');
            return EMPTY;
          })
        );
      })
    )
  );

  const initPlaid = rxMethod<void>(
    pipe(
      tap(() => store.setInitializing()),
      switchMap(() =>
        bankSyncService.getLinkToken().pipe(
          switchMap(res =>
            plaidService.prepare({
              token: res.linkToken,
              onSuccess: (publicToken, metadata) => exchangePlaidToken({publicToken, metadata}),
            })
          ),
          tap(() => store.setReady()),
          catchError((err: unknown) => {
            store.setError(ErrorUtils.extractCode(err));
            return EMPTY;
          })
        )
      )
    )
  );

  const connectMonobank = rxMethod<string>(
    pipe(
      tap(() => store.setSyncing('Connecting your Monobank account...')),
      switchMap(token =>
        bankSyncService.connectMonobank(token).pipe(
          tap(() => pollForActive()),
          catchError((err: unknown) => {
            store.setError(ErrorUtils.extractCode(err));
            return EMPTY;
          })
        )
      )
    )
  );

  const connectBinance = rxMethod<ConnectBinanceRequest>(
    pipe(
      tap(() => store.setSyncing('Connecting your Binance account...')),
      switchMap(request =>
        binanceService.connect(request).pipe(
          tap(() => pollForActive()),
          catchError((err: unknown) => {
            store.setError(ErrorUtils.extractCode(err));
            return EMPTY;
          })
        )
      )
    )
  );

  const connectIBKR = rxMethod<ConnectIBKRRequest>(
    pipe(
      tap(() => store.setSyncing('Connecting your IBKR account...')),
      switchMap(request =>
        ibkrService.connect(request).pipe(
          tap(() => pollForActive()),
          catchError((err: unknown) => {
            store.setError(ErrorUtils.extractCode(err));
            return EMPTY;
          })
        )
      )
    )
  );

  return {
    initPlaid,
    openPlaid(): void {
      plaidService.open();
    },
    destroyPlaid(): void {
      plaidService.destroy();
    },
    connectMonobank,
    connectBinance,
    connectIBKR,
    exchangePlaidToken,
    pollForActive,
  };
}

interface HookStore {
  initPlaid: () => void;
  destroyPlaid: () => void;
}

export function connectOnInit(store: HookStore): void {
  store.initPlaid();
}

export function connectOnDestroy(store: HookStore): void {
  store.destroyPlaid();
}
