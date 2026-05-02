import {CurrencyPipe} from '@angular/common';
import {type EnvironmentProviders, makeEnvironmentProviders} from '@angular/core';

import {AppCurrencyPipe} from '../pipes/app-currency.pipe';

export function provideCurrencyPipe(): EnvironmentProviders {
  return makeEnvironmentProviders([{provide: CurrencyPipe, useClass: AppCurrencyPipe}]);
}
