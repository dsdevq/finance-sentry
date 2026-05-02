import {withAsyncStatus} from '@dsdevq-common/core';
import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {dashboardComputed} from './dashboard.computed';
import {dashboardEffects, dashboardHooks} from './dashboard.effects';
import {dashboardMethods} from './dashboard.methods';
import {initialDashboardState} from './dashboard.state';

export const DashboardStore = signalStore(
  withState(initialDashboardState),
  withAsyncStatus({defaultErrorMessage: 'Failed to load dashboard data. Please try again.'}),
  withMethods(dashboardMethods),
  withComputed(dashboardComputed),
  withMethods(dashboardEffects),
  withHooks({onInit: dashboardHooks})
);
