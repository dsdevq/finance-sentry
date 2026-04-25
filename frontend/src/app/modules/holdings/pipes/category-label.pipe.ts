import {Pipe, type PipeTransform} from '@angular/core';

import {type AccountCategory} from '../../../shared/models/wealth/wealth.model';

const LABEL_MAP: Record<AccountCategory, string> = {
  banking: 'Banking',
  brokerage: 'Brokerage & Investment',
  crypto: 'Digital Assets',
  other: 'Other',
};

@Pipe({name: 'categoryLabel'})
export class CategoryLabelPipe implements PipeTransform {
  public transform(category: AccountCategory): string {
    return LABEL_MAP[category];
  }
}
