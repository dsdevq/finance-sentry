import {type EnvironmentProviders, ErrorHandler, makeEnvironmentProviders} from '@angular/core';

import {HttpErrorHandler} from '../handlers/http-error.handler';

export function provideErrorHandler(): EnvironmentProviders {
  return makeEnvironmentProviders([{provide: ErrorHandler, useClass: HttpErrorHandler}]);
}
