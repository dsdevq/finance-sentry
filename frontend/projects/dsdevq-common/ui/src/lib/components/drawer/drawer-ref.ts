import {type OverlayRef} from '@angular/cdk/overlay';
import {Injectable} from '@angular/core';
import {Subject, type Observable} from 'rxjs';

const CLOSE_ANIMATION_MS = 220;

@Injectable()
export class CmnDrawerRef<R = unknown> {
  private readonly beforeCloseSub = new Subject<void>();
  private readonly closedSub = new Subject<R | undefined>();
  private isClosed = false;

  public readonly beforeClose$: Observable<void> = this.beforeCloseSub.asObservable();

  public overlayRef!: OverlayRef;

  public close(result?: R): void {
    if (this.isClosed) {
      return;
    }
    this.isClosed = true;
    this.beforeCloseSub.next();
    this.beforeCloseSub.complete();
    setTimeout(() => {
      this.overlayRef.dispose();
      this.closedSub.next(result);
      this.closedSub.complete();
    }, CLOSE_ANIMATION_MS);
  }

  public afterClosed(): Observable<R | undefined> {
    return this.closedSub.asObservable();
  }
}
