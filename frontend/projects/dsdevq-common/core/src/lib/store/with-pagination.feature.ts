import {computed} from '@angular/core';
import {
  patchState,
  signalStoreFeature,
  withComputed,
  withMethods,
  withState,
} from '@ngrx/signals';

export interface PaginationState {
  totalCount: number;
  offset: number;
  limit: number;
}

export interface Pagination extends PaginationState {
  hasMore: boolean;
}

const DEFAULT_PAGE_SIZE = 50;

export function withPagination(defaultLimit = DEFAULT_PAGE_SIZE) {
  const initial: PaginationState = {totalCount: 0, offset: 0, limit: defaultLimit};

  return signalStoreFeature(
    withState(initial),
    withComputed(store => ({
      pagination: computed<Pagination>(() => ({
        totalCount: store.totalCount(),
        offset: store.offset(),
        limit: store.limit(),
        hasMore: store.offset() + store.limit() < store.totalCount(),
      })),
      currentPage: computed(() =>
        store.limit() > 0 ? Math.floor(store.offset() / store.limit()) + 1 : 1
      ),
      totalPages: computed(() =>
        store.limit() > 0 ? Math.max(1, Math.ceil(store.totalCount() / store.limit())) : 1
      ),
    })),
    withMethods(store => ({
      nextPage(): void {
        const next = store.offset() + store.limit();
        if (next < store.totalCount()) {
          patchState(store, {offset: next});
        }
      },
      previousPage(): void {
        patchState(store, {offset: Math.max(0, store.offset() - store.limit())});
      },
      goToPage(page: number): void {
        patchState(store, {offset: Math.max(0, (page - 1) * store.limit())});
      },
      setTotalCount(count: number): void {
        patchState(store, {totalCount: count});
      },
      setLimit(limit: number): void {
        patchState(store, {limit, offset: 0});
      },
      resetOffset(): void {
        patchState(store, {offset: 0});
      },
    }))
  );
}
