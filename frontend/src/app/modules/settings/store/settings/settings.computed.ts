import {computed, type Signal} from '@angular/core';

import {type UserProfile} from '../../models/settings/settings.model';

interface StateSignals {
  profile: Signal<Nullable<UserProfile>>;
  profileSaving: Signal<boolean>;
  passwordSaving: Signal<boolean>;
  showDeleteConfirm: Signal<boolean>;
}

export function settingsComputed(store: StateSignals) {
  return {
    avatarInitials: computed(() => {
      const p = store.profile();
      if (!p) {
        return '?';
      }
      return `${p.firstName[0] ?? ''}${p.lastName[0] ?? ''}`.toUpperCase();
    }),
    fullName: computed(() => {
      const p = store.profile();
      return p ? `${p.firstName} ${p.lastName}` : '';
    }),
  };
}
