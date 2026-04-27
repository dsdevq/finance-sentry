import {DialogRef} from '@angular/cdk/dialog';
import {TestBed} from '@angular/core/testing';
import {CMN_DIALOG_DATA} from '@dsdevq-common/ui';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type DisconnectDialogData} from '../../models/disconnect-dialog/disconnect-dialog.model';
import {DisconnectDialogComponent} from './disconnect-dialog.component';

describe('DisconnectDialogComponent', () => {
  const data: DisconnectDialogData = {providerName: 'Plaid'};
  let dialogRef: {close: ReturnType<typeof vi.fn>};

  beforeEach(() => {
    dialogRef = {close: vi.fn()};
    TestBed.configureTestingModule({
      providers: [
        {provide: DialogRef, useValue: dialogRef},
        {provide: CMN_DIALOG_DATA, useValue: data},
      ],
    });
  });

  it('exposes the injected provider name on data', () => {
    const fixture = TestBed.createComponent(DisconnectDialogComponent);
    expect(fixture.componentInstance.data).toEqual(data);
  });

  it('confirm() closes with true', () => {
    const fixture = TestBed.createComponent(DisconnectDialogComponent);
    fixture.componentInstance.confirm();
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('cancel() closes with false', () => {
    const fixture = TestBed.createComponent(DisconnectDialogComponent);
    fixture.componentInstance.cancel();
    expect(dialogRef.close).toHaveBeenCalledWith(false);
  });

  it('renders the provider name in the dialog body', () => {
    const fixture = TestBed.createComponent(DisconnectDialogComponent);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Plaid');
  });
});
