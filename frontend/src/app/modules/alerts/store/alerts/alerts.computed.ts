import {computed, type Signal} from '@angular/core';

import {type Alert, type AlertFilter} from '../../models/alert/alert.model';

interface StateSignals {
  alerts: Signal<Alert[]>;
  filter: Signal<AlertFilter>;
}

export function alertsComputed(store: StateSignals) {
  return {
    unreadCount: computed(() => store.alerts().filter(a => !a.read).length),
    errorCount: computed(() => store.alerts().filter(a => a.severity === 'error').length),
    warningCount: computed(() => store.alerts().filter(a => a.severity === 'warning').length),
    filtered: computed((): Alert[] => {
      const f = store.filter();
      const all = store.alerts();
      if (f === 'unread') {
        return all.filter(a => !a.read);
      }
      if (f === 'error') {
        return all.filter(a => a.severity === 'error');
      }
      if (f === 'warning') {
        return all.filter(a => a.severity === 'warning');
      }
      if (f === 'info') {
        return all.filter(a => a.severity === 'info');
      }
      return all;
    }),
  };
}
