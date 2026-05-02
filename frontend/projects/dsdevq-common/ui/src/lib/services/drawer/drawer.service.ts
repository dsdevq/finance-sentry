import {Overlay} from '@angular/cdk/overlay';
import {ComponentPortal, type ComponentType} from '@angular/cdk/portal';
import {inject, Injectable, Injector} from '@angular/core';

import {CmnDrawerContainerComponent} from '../../components/drawer/drawer-container.component';
import {CMN_DRAWER_DATA, type CmnDrawerOpenConfig} from '../../components/drawer/drawer-config';
import {CmnDrawerRef} from '../../components/drawer/drawer-ref';

@Injectable({providedIn: 'root'})
export class CmnDrawerService {
  private readonly overlay = inject(Overlay);
  private readonly injector = inject(Injector);

  public open<R = unknown, D = unknown, C = unknown>(
    component: ComponentType<C>,
    config: CmnDrawerOpenConfig<D> = {}
  ): CmnDrawerRef<R> {
    const drawerRef = new CmnDrawerRef<R>();

    const overlayRef = this.overlay.create({
      width: config.width ?? '480px',
      height: '100%',
      positionStrategy: this.overlay.position().global().right().top(),
      hasBackdrop: true,
      backdropClass: 'cmn-drawer-backdrop',
      panelClass: 'cmn-drawer-panel',
      scrollStrategy: this.overlay.scrollStrategies.block(),
    });

    drawerRef.overlayRef = overlayRef;

    overlayRef.backdropClick().subscribe(() => {
      if (!config.disableClose) {
        drawerRef.close();
      }
    });

    overlayRef.keydownEvents().subscribe(e => {
      if (e.key === 'Escape' && !config.disableClose) {
        drawerRef.close();
      }
    });

    const childInjector = Injector.create({
      providers: [
        {provide: CmnDrawerRef, useValue: drawerRef},
        {provide: CMN_DRAWER_DATA, useValue: config.data ?? null},
      ],
      parent: this.injector,
    });

    const containerPortal = new ComponentPortal(CmnDrawerContainerComponent, null, childInjector);
    const containerRef = overlayRef.attach(containerPortal);

    containerRef.instance.title.set(config.title ?? '');
    containerRef.changeDetectorRef.detectChanges();

    const contentPortal = new ComponentPortal(component, null, childInjector);
    containerRef.instance.portalOutlet().attach(contentPortal);

    return drawerRef;
  }
}
