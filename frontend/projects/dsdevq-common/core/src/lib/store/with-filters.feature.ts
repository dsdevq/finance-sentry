import {computed} from '@angular/core';
import {
  patchState,
  signalStoreFeature,
  withComputed,
  withMethods,
  withState,
} from '@ngrx/signals';

export function withFilters<TFilters extends object>(initialFilters: TFilters) {
  return signalStoreFeature(
    withState({filters: initialFilters}),
    withComputed(store => ({
      hasActiveFilters: computed(
        () => JSON.stringify(store.filters()) !== JSON.stringify(initialFilters)
      ),
    })),
    withMethods(store => ({
      setFilters(patch: Partial<TFilters>): void {
        patchState(store, {filters: {...store.filters(), ...patch}});
      },
      resetFilters(): void {
        patchState(store, {filters: initialFilters});
      },
    }))
  );
}
