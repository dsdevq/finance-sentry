import {withAsyncStatus} from '@lifekit-hq/core';
import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {incomeComputed} from './income.computed';
import {incomeEffects, incomeHooks} from './income.effects';
import {incomeMethods} from './income.methods';
import {initialIncomeState} from './income.state';

export const IncomeStore = signalStore(
  withState(initialIncomeState),
  withAsyncStatus({defaultErrorMessage: 'Failed to load income data. Please try again.'}),
  withMethods(incomeMethods),
  withComputed(incomeComputed),
  withMethods(incomeEffects),
  withHooks({onInit: incomeHooks})
);
