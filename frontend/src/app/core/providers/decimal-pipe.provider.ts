import {DecimalPipe} from '@angular/common';
import {type EnvironmentProviders, makeEnvironmentProviders} from '@angular/core';

import {AppDecimalPipe} from '../pipes/app-decimal.pipe';

export function provideDecimalPipe(): EnvironmentProviders {
  return makeEnvironmentProviders([{provide: DecimalPipe, useClass: AppDecimalPipe}]);
}
