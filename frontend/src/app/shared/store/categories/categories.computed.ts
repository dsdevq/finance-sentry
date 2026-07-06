import {computed, type Signal} from '@angular/core';

import {type CategoryModel} from '../../models/category/category.model';

interface ComputedStore {
  categories: Signal<CategoryModel[]>;
}

/** Presentation palette assigned to categories by order — not part of the taxonomy. */
const CATEGORY_PALETTE: readonly string[] = [
  '#818cf8',
  '#f59e0b',
  '#10b981',
  '#f97316',
  '#8b5cf6',
  '#4f46e5',
  '#06b6d4',
  '#ec4899',
  '#ef4444',
  '#14b8a6',
  '#a855f7',
  '#eab308',
  '#64748b',
  '#22c55e',
  '#0ea5e9',
  '#f43f5e',
];

export const CATEGORY_COLOR_FALLBACK = '#94a3b8';

export function categoriesComputed(store: ComputedStore) {
  return {
    labelMap: computed(() => {
      const map: Record<string, string> = {};
      for (const category of store.categories()) {
        map[category.key] = category.label;
      }
      return map;
    }),
    filterOptions: computed(() =>
      store.categories().map(category => ({value: category.key, label: category.label}))
    ),
    colorMap: computed(() => {
      const map: Record<string, string> = {};
      store.categories().forEach((category, index) => {
        map[category.key] = CATEGORY_PALETTE[index % CATEGORY_PALETTE.length];
      });
      return map;
    }),
  };
}
