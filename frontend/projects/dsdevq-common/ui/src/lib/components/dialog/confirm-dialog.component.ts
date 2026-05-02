import {DialogRef} from '@angular/cdk/dialog';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';

import {ButtonComponent, type ButtonVariant} from '../button/button.component';

import {CMN_DIALOG_DATA} from './dialog-config';
import {DialogActionsComponent} from './dialog-actions.component';

export interface ConfirmDialogData {
  title?: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmVariant?: ButtonVariant;
}

@Component({
  selector: 'cmn-confirm-dialog',
  imports: [ButtonComponent, DialogActionsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data.title) {
      <h2 class="font-headline text-base font-semibold text-text-primary mb-cmn-2">
        {{ data.title }}
      </h2>
    }
    <p class="text-cmn-sm text-text-secondary">{{ data.message }}</p>
    <cmn-dialog-actions>
      <cmn-button (clicked)="cancel()" variant="secondary">
        {{ data.cancelLabel ?? 'Cancel' }}
      </cmn-button>
      <cmn-button (clicked)="confirm()" [variant]="data.confirmVariant ?? 'primary'">
        {{ data.confirmLabel ?? 'Confirm' }}
      </cmn-button>
    </cmn-dialog-actions>
  `,
})
export class ConfirmDialogComponent {
  private readonly ref = inject<DialogRef<boolean>>(DialogRef);
  protected readonly data = inject<ConfirmDialogData>(CMN_DIALOG_DATA);

  protected confirm(): void {
    this.ref.close(true);
  }

  protected cancel(): void {
    this.ref.close(false);
  }
}
