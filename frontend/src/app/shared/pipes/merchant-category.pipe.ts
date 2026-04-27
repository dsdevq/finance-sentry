import {Pipe, type PipeTransform} from '@angular/core';

import {MerchantCategoryUtils} from '../utils/merchant-category.utils';

@Pipe({name: 'merchantCategory'})
export class MerchantCategoryPipe implements PipeTransform {
  public transform(category: string): string {
    return MerchantCategoryUtils.format(category);
  }
}
