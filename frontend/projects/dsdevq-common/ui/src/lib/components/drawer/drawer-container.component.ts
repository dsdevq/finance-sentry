import {CdkPortalOutlet} from '@angular/cdk/portal';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';

import {IconComponent} from '../icon/icon.component';
import {CmnDrawerRef} from './drawer-ref';

type DrawerState = 'entering' | 'open' | 'closing';

@Component({
  selector: 'cmn-drawer-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CdkPortalOutlet, IconComponent],
  host: {
    '[class.cmn-drawer--open]': 'isOpen',
    '[class.cmn-drawer--closing]': 'isClosing',
  },
  template: `
    <div class="flex h-full flex-col bg-surface-card" style="min-width: 0">
      <!-- Header -->
      <div
        class="flex shrink-0 items-center justify-between border-b border-border-default px-cmn-6 py-cmn-4"
      >
        <h2 class="text-cmn-base font-semibold text-text-primary">{{ title() }}</h2>
        <button
          (click)="drawerRef.close()"
          class="flex h-8 w-8 items-center justify-center rounded-cmn-md text-text-secondary transition-colors hover:bg-surface-raised hover:text-text-primary"
          aria-label="Close drawer"
        >
          <cmn-icon name="X" size="sm" />
        </button>
      </div>
      <!-- Body -->
      <div class="flex-1 overflow-y-auto">
        <ng-template cdkPortalOutlet />
      </div>
    </div>
  `,
})
export class CmnDrawerContainerComponent implements AfterViewInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private state: DrawerState = 'entering';

  public readonly title = signal('');
  public readonly drawerRef = inject(CmnDrawerRef);
  public readonly portalOutlet = viewChild.required(CdkPortalOutlet);

  public get isOpen(): boolean {
    return this.state === 'open';
  }

  public get isClosing(): boolean {
    return this.state === 'closing';
  }

  public ngAfterViewInit(): void {
    requestAnimationFrame(() => {
      this.state = 'open';
      this.cdr.markForCheck();
    });

    this.drawerRef.beforeClose$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.state = 'closing';
      this.cdr.markForCheck();
    });
  }
}
