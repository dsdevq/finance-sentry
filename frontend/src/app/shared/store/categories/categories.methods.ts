import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type CategoryModel} from '../../models/category/category.model';
import {type CategoriesState} from './categories.state';

export function categoriesMethods(store: WritableStateSource<CategoriesState>) {
  return {
    setCategories(categories: CategoryModel[]): void {
      patchState(store, {categories, loaded: true});
    },
  };
}
