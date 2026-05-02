import {InjectionToken} from '@angular/core';

export type ErrorMessagesMap = Record<string, string>;

export const ERROR_MESSAGES = new InjectionToken<ErrorMessagesMap>('ERROR_MESSAGES', {
  providedIn: 'root',
  factory: () => ({}),
});
