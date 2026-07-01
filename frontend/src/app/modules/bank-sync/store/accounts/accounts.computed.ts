import {computed, type Signal} from '@angular/core';

import {
  type CategorySummary,
  type InstitutionGroup,
  type WealthSummaryResponse,
} from '../../../../shared/models/wealth/wealth.model';
import {InstitutionUtils} from '../../../../shared/utils/institution.utils';

interface StateSignals {
  summary: Signal<Nullable<WealthSummaryResponse>>;
  status: Signal<'idle' | 'loading' | 'success' | 'error'>;
}

export function accountsComputed(store: StateSignals) {
  const bankingCategory = computed<Nullable<CategorySummary>>(
    () => store.summary()?.categories.find(c => c.category === 'banking') ?? null
  );
  const cryptoCategory = computed<Nullable<CategorySummary>>(
    () => store.summary()?.categories.find(c => c.category === 'crypto') ?? null
  );
  const brokerageCategory = computed<Nullable<CategorySummary>>(
    () => store.summary()?.categories.find(c => c.category === 'brokerage') ?? null
  );

  return {
    isEmpty: computed(
      () => store.status() === 'success' && (store.summary()?.categories ?? []).length === 0
    ),
    totalNetWorth: computed(() => store.summary()?.totalNetWorth ?? 0),
    baseCurrency: computed(() => store.summary()?.baseCurrency ?? 'USD'),
    bankingCategory,
    cryptoCategory,
    brokerageCategory,
    bankingInstitutions: computed<InstitutionGroup[]>(() =>
      InstitutionUtils.groupByInstitution(bankingCategory()?.accounts ?? [])
    ),
    cryptoInstitutions: computed<InstitutionGroup[]>(() =>
      InstitutionUtils.groupByInstitution(cryptoCategory()?.accounts ?? [])
    ),
    brokerageInstitutions: computed<InstitutionGroup[]>(() =>
      InstitutionUtils.groupByInstitution(brokerageCategory()?.accounts ?? [])
    ),
    totalConnections: computed(
      () => store.summary()?.categories.reduce((sum, cat) => sum + cat.institutionCount, 0) ?? 0
    ),
  };
}
