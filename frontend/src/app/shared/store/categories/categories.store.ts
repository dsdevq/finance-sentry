import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {categoriesComputed} from './categories.computed';
import {categoriesEffects, categoriesHooks} from './categories.effects';
import {categoriesMethods} from './categories.methods';
import {initialCategoriesState} from './categories.state';

/**
 * App-wide reference store for spending categories. Loads once from `GET /categories`
 * on first injection (authenticated pages) and is the single source of truth for
 * category labels, filter options, and colors across the frontend.
 */
export const CategoryStore = signalStore(
  {providedIn: 'root'},
  withState(initialCategoriesState),
  withMethods(categoriesMethods),
  withComputed(categoriesComputed),
  withMethods(categoriesEffects),
  withHooks({onInit: categoriesHooks})
);
