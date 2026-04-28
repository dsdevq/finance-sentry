import {CdkDialogContainer, DialogRef} from '@angular/cdk/dialog';
import {CdkPortalOutlet} from '@angular/cdk/portal';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';

import {type CmnDialogConfig, type CmnDialogSize} from './dialog-config';

const SHELL_BASE =
  'cmn-dialog-shell relative flex flex-col bg-surface-card text-text-primary ' +
  'rounded-cmn-lg border border-border-default shadow-cmn-lg overflow-hidden ' +
  'max-h-[90vh] w-full';

const SIZE_CLASSES: Record<CmnDialogSize, string> = {
  sm: 'min-w-[24rem] max-w-md',
  md: 'min-w-[32rem] max-w-xl',
  lg: 'min-w-[40rem] max-w-3xl',
  full: 'max-w-[95vw] h-[95vh]',
};

@Component({
  selector: 'cmn-dialog-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CdkPortalOutlet],
  template: `
    <section
      [class]="shellClasses()"
      [attr.aria-modal]="'true'"
      [attr.aria-label]="config.ariaLabel || null"
      role="dialog"
    >
      @if (config.title) {
        <header
          class="cmn-dialog-shell__header flex items-center justify-between gap-cmn-3 px-cmn-6 py-cmn-4 border-b border-border-default"
        >
          <h2 class="cmn-dialog-shell__title text-cmn-lg font-medium m-0">{{ config.title }}</h2>
          <button
            (click)="onClose()"
            type="button"
            class="cmn-dialog-shell__close inline-flex items-center justify-center w-8 h-8 rounded-cmn-sm hover:bg-surface-raised focus:outline-none focus:ring-2 focus:ring-border-focus"
            aria-label="Close dialog"
          >
            ×
          </button>
        </header>
      }
      <div class="cmn-dialog-shell__body overflow-y-auto p-cmn-6 flex-1 min-h-[20rem]">
        <ng-template cdkPortalOutlet />
      </div>
    </section>
  `,
})
export class CmnDialogContainerComponent extends CdkDialogContainer<CmnDialogConfig> {
  private readonly dialogRef = inject<DialogRef<unknown, unknown>>(DialogRef, {optional: true});

  public get config(): CmnDialogConfig {
    return this._config;
  }

  protected shellClasses(): string {
    return `${SHELL_BASE} ${SIZE_CLASSES[this._config.size ?? 'md']}`;
  }

  protected onClose(): void {
    if (!this._config.disableClose) {
      this.dialogRef?.close();
    }
  }
}
