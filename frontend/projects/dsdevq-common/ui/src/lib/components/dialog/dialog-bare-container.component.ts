import {CdkDialogContainer} from '@angular/cdk/dialog';
import {CdkPortalOutlet} from '@angular/cdk/portal';
import {ChangeDetectionStrategy, Component} from '@angular/core';

@Component({
  selector: 'cmn-dialog-bare-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CdkPortalOutlet],
  template: '<ng-template cdkPortalOutlet />',
})
export class CmnDialogBareContainerComponent extends CdkDialogContainer {}
