import {type DialogRef} from '@angular/cdk/dialog';
import {type Observable} from 'rxjs';

export class CmnDialogRef<R = unknown, C = unknown> {
  public constructor(private readonly cdkRef: DialogRef<R, C>) {}

  public get componentInstance(): C | null {
    return this.cdkRef.componentInstance;
  }

  public close(result?: R): void {
    this.cdkRef.close(result);
  }

  public afterClosed(): Observable<R | undefined> {
    return this.cdkRef.closed;
  }
}
