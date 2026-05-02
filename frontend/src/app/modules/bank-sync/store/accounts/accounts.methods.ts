import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type WealthSummaryResponse} from '../../../../shared/models/wealth/wealth.model';
import {type AccountsState} from './accounts.state';

export function accountsMethods(store: WritableStateSource<AccountsState>) {
  return {
    setSummary(summary: WealthSummaryResponse): void {
      patchState(store, {summary});
    },
  };
}
