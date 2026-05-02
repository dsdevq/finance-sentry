import {CdkDialogContainer} from '@angular/cdk/dialog';
import {DialogConfig} from '@angular/cdk/dialog';
import {type ComponentType} from '@angular/cdk/portal';
import {type Injector, InjectionToken, type ViewContainerRef} from '@angular/core';

export type CmnDialogSize = 'sm' | 'md' | 'lg' | 'full';
export type CmnDialogAutoFocus = 'first-tabbable' | 'first-heading' | 'dialog' | false;

export interface CmnDialogOpenConfig<D = unknown> {
  data?: D;
  disableClose?: boolean;
  title?: string;
  size?: CmnDialogSize;
  ariaLabel?: string;
  autoFocus?: CmnDialogAutoFocus;
  viewContainerRef?: ViewContainerRef;
  injector?: Injector;
  // Overrides for custom dialogs that manage their own overlay/chrome.
  container?: ComponentType<CdkDialogContainer>;
  hasBackdrop?: boolean;
  panelClass?: string | string[];
}

export class CmnDialogConfig<D = unknown> extends DialogConfig<D> {
  public title?: string;
  public size: CmnDialogSize = 'md';
}

export const CMN_DIALOG_DATA = new InjectionToken<unknown>('CmnDialogData');
