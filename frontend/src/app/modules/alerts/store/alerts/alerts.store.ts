import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {alertsComputed} from './alerts.computed';
import {alertsEffects, alertsHooks} from './alerts.effects';
import {alertsMethods} from './alerts.methods';
import {initialAlertsState} from './alerts.state';

export const AlertsStore = signalStore(
  withState(initialAlertsState),
  withMethods(alertsMethods),
  withComputed(alertsComputed),
  withMethods(alertsEffects),
  withHooks({onInit: alertsHooks})
);
