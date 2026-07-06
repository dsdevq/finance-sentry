import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap} from 'rxjs';

import {type CategoryModel} from '../../models/category/category.model';
import {CategoryService} from '../../services/category.service';

interface EffectsStore {
  setCategories: (categories: CategoryModel[]) => void;
}

export function categoriesEffects(store: EffectsStore) {
  const categoryService = inject(CategoryService);

  const load = rxMethod<void>(
    pipe(
      switchMap(() =>
        categoryService.getCategories().pipe(
          tap(categories => store.setCategories(categories)),
          catchError(() => EMPTY)
        )
      )
    )
  );

  return {load};
}

interface HookStore {
  load: () => void;
  loaded: () => boolean;
}

export function categoriesHooks(store: HookStore): void {
  if (!store.loaded()) {
    store.load();
  }
}
