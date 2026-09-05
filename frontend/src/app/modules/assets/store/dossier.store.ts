import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {dossierComputed} from './dossier.computed';
import {dossierEffects, dossierHooks} from './dossier.effects';
import {dossierMethods} from './dossier.methods';
import {initialDossierState} from './dossier.state';

export const DossierStore = signalStore(
  withState(initialDossierState),
  withMethods(dossierMethods),
  withComputed(dossierComputed),
  withMethods(dossierEffects),
  withHooks(dossierHooks)
);
