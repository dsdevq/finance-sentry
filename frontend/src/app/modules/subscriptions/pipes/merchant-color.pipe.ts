import {Pipe, PipeTransform} from '@angular/core';

import {SubscriptionUtils} from '../utils/subscription.utils';

@Pipe({name: 'merchantColor'})
export class MerchantColorPipe implements PipeTransform {
  public transform(name: string): string {
    return SubscriptionUtils.getMerchantColor(name);
  }
}
