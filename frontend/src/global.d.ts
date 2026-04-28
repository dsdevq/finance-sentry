/// <reference path="../projects/dsdevq-common/ui/src/global.d.ts" />

import type {
  PlaidHandler,
  PlaidLinkOptions,
} from './app/modules/bank-sync/models/plaid/plaid.model';

declare global {
  interface Window {
    Plaid?: {
      create: (options: PlaidLinkOptions) => PlaidHandler;
    };
  }
}
