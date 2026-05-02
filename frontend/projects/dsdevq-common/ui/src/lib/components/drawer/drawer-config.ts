import {InjectionToken} from '@angular/core';

export interface CmnDrawerOpenConfig<D = unknown> {
  data?: D;
  title?: string;
  width?: string;
  disableClose?: boolean;
}

export const CMN_DRAWER_DATA = new InjectionToken<unknown>('CmnDrawerData');
