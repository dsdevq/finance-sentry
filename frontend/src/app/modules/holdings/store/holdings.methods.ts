import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type WealthSummaryResponse} from '../../bank-sync/models/wealth.model';
import {type HoldingsState} from './holdings.state';

export function holdingsMethods(store: WritableStateSource<HoldingsState>) {
  return {
    setLoading(): void {
      patchState(store, {status: 'loading', errorCode: null});
    },
    setSummary(summary: WealthSummaryResponse): void {
      patchState(store, {summary, status: 'idle', errorCode: null});
    },
    setError(errorCode: string | null): void {
      patchState(store, {status: 'error', errorCode});
    },
  };
}
