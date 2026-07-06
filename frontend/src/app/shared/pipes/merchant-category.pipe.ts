import {inject, Pipe, type PipeTransform} from '@angular/core';

import {CategoryStore} from '../store/categories/categories.store';
import {MerchantCategoryUtils} from '../utils/merchant-category.utils';

/**
 * Resolves a category key to its display label from the {@link CategoryStore}
 * (single source of truth), falling back to a mechanical format of the key when
 * the reference list has not loaded yet. Impure so labels refresh once loaded.
 */
@Pipe({name: 'merchantCategory', pure: false})
export class MerchantCategoryPipe implements PipeTransform {
  private readonly categoryStore = inject(CategoryStore);

  public transform(category: string): string {
    return this.categoryStore.labelMap()[category] ?? MerchantCategoryUtils.format(category);
  }
}
