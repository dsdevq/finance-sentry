import {DialogRef} from '@angular/cdk/dialog';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {ButtonComponent, CMN_DIALOG_DATA, DialogActionsComponent} from '@dsdevq-common/ui';

export interface DisconnectDialogData {
  readonly providerName: string;
}

@Component({
  selector: 'fns-disconnect-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonComponent, DialogActionsComponent],
  templateUrl: './disconnect-dialog.component.html',
})
export class DisconnectDialogComponent {
  private readonly dialogRef = inject<DialogRef<boolean>>(DialogRef);

  public readonly data = inject<DisconnectDialogData>(CMN_DIALOG_DATA);

  public confirm(): void {
    this.dialogRef.close(true);
  }

  public cancel(): void {
    this.dialogRef.close(false);
  }
}
