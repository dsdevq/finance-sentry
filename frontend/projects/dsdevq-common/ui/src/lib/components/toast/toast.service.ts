import {Overlay, OverlayRef} from '@angular/cdk/overlay';
import {ComponentPortal} from '@angular/cdk/portal';
import {inject, Injectable, Injector} from '@angular/core';

import {DEFAULT_DURATION_MS, ToastComponent, type ToastVariant} from './toast.component';

@Injectable({providedIn: 'root'})
export class ToastService {
  private readonly overlay = inject(Overlay);
  private readonly injector = inject(Injector);

  public show(message: string, variant: ToastVariant, duration = DEFAULT_DURATION_MS): void {
    const positionStrategy = this.overlay.position().global().centerHorizontally().bottom('24px');
    const overlayRef: OverlayRef = this.overlay.create({positionStrategy});

    const portal = new ComponentPortal(ToastComponent, null, this.injector);
    const ref = overlayRef.attach(portal);

    ref.setInput('message', message);
    ref.setInput('variant', variant);

    const dispose = (): void => {
      if (overlayRef.hasAttached()) {
        overlayRef.dispose();
      }
    };

    ref.instance.dismissed.subscribe(dispose);
    setTimeout(dispose, duration);
  }

  public success(message: string, duration?: number): void {
    this.show(message, 'success', duration);
  }

  public error(message: string, duration?: number): void {
    this.show(message, 'error', duration);
  }

  public warning(message: string, duration?: number): void {
    this.show(message, 'warning', duration);
  }

  public info(message: string, duration?: number): void {
    this.show(message, 'info', duration);
  }
}
