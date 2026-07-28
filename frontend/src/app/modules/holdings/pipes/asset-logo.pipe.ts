import {Pipe, type PipeTransform} from '@angular/core';

import {AssetLogoUtils} from '../utils/asset-logo.utils';

@Pipe({name: 'assetLogo'})
export class AssetLogoPipe implements PipeTransform {
  public transform(symbol: Nullable<string>, provider?: Nullable<string>): Nullable<string> {
    return AssetLogoUtils.logoUrl(symbol, provider ?? null);
  }
}
