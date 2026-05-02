import {patchState, signalStoreFeature, withMethods, withState} from '@ngrx/signals';

export type SortDirection = 'asc' | 'desc';

export interface Sort<TField extends string = string> {
  field: TField;
  dir: SortDirection;
}

export function withSorting<TField extends string>(defaultSort: Sort<TField>) {
  return signalStoreFeature(
    withState({sort: defaultSort as Sort<TField>}),
    withMethods(store => ({
      setSort(sort: Sort<TField>): void {
        patchState(store, {sort});
      },
      toggleSort(field: TField): void {
        const current = store.sort();
        if (current.field === field) {
          patchState(store, {sort: {field, dir: current.dir === 'asc' ? 'desc' : 'asc'}});
        } else {
          patchState(store, {sort: {field, dir: 'asc'}});
        }
      },
      resetSort(): void {
        patchState(store, {sort: defaultSort});
      },
    }))
  );
}
