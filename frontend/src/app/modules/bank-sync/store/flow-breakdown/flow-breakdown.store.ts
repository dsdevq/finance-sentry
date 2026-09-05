import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {flowBreakdownComputed} from './flow-breakdown.computed';
import {flowBreakdownEffects, flowBreakdownHooks} from './flow-breakdown.effects';
import {flowBreakdownMethods} from './flow-breakdown.methods';
import {initialFlowBreakdownState} from './flow-breakdown.state';

export const FlowBreakdownStore = signalStore(
  withState(initialFlowBreakdownState),
  withMethods(flowBreakdownMethods),
  withComputed(flowBreakdownComputed),
  withMethods(flowBreakdownEffects),
  withHooks({onInit: flowBreakdownHooks})
);
