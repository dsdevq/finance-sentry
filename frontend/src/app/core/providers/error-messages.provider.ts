import {type EnvironmentProviders, makeEnvironmentProviders} from '@angular/core';
import {ERROR_MESSAGES} from '@dsdevq-common/ui';

import {ERROR_MESSAGES_REGISTRY} from '../errors/error-messages.registry';

export function provideErrorMessages(): EnvironmentProviders {
  return makeEnvironmentProviders([{provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY}]);
}
