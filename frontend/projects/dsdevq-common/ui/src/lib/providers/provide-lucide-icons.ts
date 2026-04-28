import {type EnvironmentProviders, makeEnvironmentProviders} from '@angular/core';
import {LUCIDE_ICONS, LucideIconProvider, type LucideIcons} from 'lucide-angular';

export function provideLucideIcons(map: LucideIcons): EnvironmentProviders {
  return makeEnvironmentProviders([
    {
      provide: LUCIDE_ICONS,
      multi: true,
      useValue: new LucideIconProvider(map),
    },
  ]);
}
