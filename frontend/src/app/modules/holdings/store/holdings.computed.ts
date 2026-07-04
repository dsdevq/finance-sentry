import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/core';

import {type CategorySummary, type Institution} from '../../../shared/models/wealth/wealth.model';
import {type Position} from '../models/position/position.model';
import {type HoldingsState} from './holdings.state';

interface StateSignals {
  summary: Signal<HoldingsState['summary']>;
  status: Signal<HoldingsState['status']>;
  errorCode: Signal<Nullable<string>>;
  positions: Signal<Position[]>;
  positionsStatus: Signal<HoldingsState['positionsStatus']>;
  positionsErrorCode: Signal<Nullable<string>>;
}

const DEFAULT_ERROR = 'Failed to load holdings. Please try again.';
const DEFAULT_POSITIONS_ERROR = 'Failed to load positions.';
const WEIGHT_TO_PERCENT = 100;

export type AssetClass = 'equity' | 'crypto';

export interface PositionRow {
  symbol: string;
  provider: string;
  quantity: number;
  currentPrice: number;
  currentValue: number;
  pnlPercent: number;
  weightPercent: number;
}

export interface PositionAssetGroup {
  assetClass: AssetClass;
  label: string;
  rows: PositionRow[];
  totalValue: number;
}

const ASSET_CLASS_ORDER: readonly AssetClass[] = ['equity', 'crypto'];

const ASSET_CLASS_LABEL: Record<AssetClass, string> = {
  equity: 'Equities',
  crypto: 'Crypto',
};

const CRYPTO_PROVIDERS = new Set<string>(['binance']);

function resolveAssetClass(provider: string): AssetClass {
  return CRYPTO_PROVIDERS.has(provider) ? 'crypto' : 'equity';
}

export function holdingsComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isLoading: computed(() => store.status() === 'loading'),
    errorMessage: computed(() => {
      if (store.status() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.errorCode()) ?? DEFAULT_ERROR;
    }),
    totalNetWorth: computed(() => store.summary()?.totalNetWorth ?? 0),
    baseCurrency: computed(() => store.summary()?.baseCurrency ?? 'USD'),
    bankingCategory: computed(
      (): Nullable<CategorySummary> =>
        store.summary()?.categories.find(c => c.category === 'banking') ?? null
    ),
    brokerageCategory: computed(
      (): Nullable<CategorySummary> =>
        store.summary()?.categories.find(c => c.category === 'brokerage') ?? null
    ),
    cryptoCategory: computed(
      (): Nullable<CategorySummary> =>
        store.summary()?.categories.find(c => c.category === 'crypto') ?? null
    ),
    allInstitutions: computed(
      (): Institution[] => store.summary()?.categories.flatMap(c => c.institutions) ?? []
    ),
    positions: computed((): Position[] => store.positions()),
    isPositionsLoading: computed(() => store.positionsStatus() === 'loading'),
    positionsLoaded: computed(
      () => store.positionsStatus() !== 'idle' || store.positions().length > 0
    ),
    positionsErrorMessage: computed(() => {
      if (store.positionsStatus() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.positionsErrorCode()) ?? DEFAULT_POSITIONS_ERROR;
    }),
    totalPositionsValue: computed(() =>
      store.positions().reduce((sum, p) => sum + p.currentValue, 0)
    ),
    positionsByAssetClass: computed(() => {
      const positions = store.positions();
      const totalValue = positions.reduce((sum, p) => sum + p.currentValue, 0);
      const groups = new Map<AssetClass, PositionRow[]>();

      for (const p of positions) {
        const assetClass = resolveAssetClass(p.provider);
        const row: PositionRow = {
          symbol: p.symbol,
          provider: p.provider,
          quantity: p.quantity,
          currentPrice: p.currentPrice,
          currentValue: p.currentValue,
          pnlPercent: p.mockPnlPercent,
          weightPercent: totalValue > 0 ? (p.currentValue / totalValue) * WEIGHT_TO_PERCENT : 0,
        };
        const bucket = groups.get(assetClass);
        if (bucket) {
          bucket.push(row);
        } else {
          groups.set(assetClass, [row]);
        }
      }

      return ASSET_CLASS_ORDER.filter(cls => groups.has(cls)).map(cls => ({
        assetClass: cls,
        label: ASSET_CLASS_LABEL[cls],
        rows: (groups.get(cls) ?? []).sort((a, b) => b.currentValue - a.currentValue),
        totalValue: (groups.get(cls) ?? []).reduce((sum, r) => sum + r.currentValue, 0),
      }));
    }),
  };
}
