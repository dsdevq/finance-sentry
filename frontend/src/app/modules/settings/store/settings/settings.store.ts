import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {settingsComputed} from './settings.computed';
import {settingsEffects, settingsHooks} from './settings.effects';
import {settingsMethods} from './settings.methods';
import {initialSettingsState} from './settings.state';

export const SettingsStore = signalStore(
  withState(initialSettingsState),
  withMethods(settingsMethods),
  withComputed(settingsComputed),
  withMethods(settingsEffects),
  withHooks({onInit: settingsHooks})
);
