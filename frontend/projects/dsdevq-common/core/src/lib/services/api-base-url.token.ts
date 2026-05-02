import {type EnvironmentProviders, InjectionToken, makeEnvironmentProviders} from '@angular/core';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');

export function provideApiBaseUrl(url: string): EnvironmentProviders {
  return makeEnvironmentProviders([{provide: API_BASE_URL, useValue: url}]);
}
