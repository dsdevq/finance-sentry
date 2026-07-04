import {
  CdkConnectedOverlay,
  CdkOverlayOrigin,
  type ConnectedPosition,
  Overlay,
} from '@angular/cdk/overlay';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';

import {IconComponent, type LucideIconName} from '../icon/icon.component';

export interface MenuItem {
  id: string;
  label: string;
  icon?: LucideIconName;
  destructive?: boolean;
  disabled?: boolean;
}

const TRIGGER_BASE_CLASSES =
  'inline-flex items-center justify-center rounded-cmn-md focus:outline-none ' +
  'focus:ring-2 focus:ring-border-focus focus:ring-offset-1';

const PANEL_BASE_CLASSES =
  'min-w-48 overflow-hidden rounded-cmn-lg border border-border-default bg-surface-card ' +
  'py-cmn-2 shadow-cmn-lg';

const ITEM_BASE_CLASSES =
  'flex w-full items-center gap-cmn-3 px-cmn-4 py-cmn-2 text-left text-cmn-sm transition-colors ' +
  'focus:outline-none focus:bg-surface-raised';

const ITEM_DEFAULT_CLASSES = 'text-text-primary hover:bg-surface-raised';
const ITEM_DESTRUCTIVE_CLASSES = 'text-status-error hover:bg-status-error/10';

const MENU_POSITIONS: ConnectedPosition[] = [
  {
    originX: 'end',
    originY: 'bottom',
    overlayX: 'end',
    overlayY: 'top',
    offsetY: 8,
  },
  {
    originX: 'end',
    originY: 'top',
    overlayX: 'end',
    overlayY: 'bottom',
    offsetY: -8,
  },
];

@Component({
  selector: 'cmn-menu',
  imports: [CdkConnectedOverlay, CdkOverlayOrigin, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      #menuOrigin="cdkOverlayOrigin"
      [attr.aria-label]="ariaLabel()"
      [attr.aria-haspopup]="'menu'"
      [attr.aria-expanded]="isOpen()"
      [class]="triggerClasses()"
      (click)="toggle()"
      cdkOverlayOrigin
      type="button"
    >
      <ng-content />
    </button>

    <ng-template
      [cdkConnectedOverlayOrigin]="menuOrigin"
      [cdkConnectedOverlayOpen]="isOpen()"
      [cdkConnectedOverlayHasBackdrop]="true"
      [cdkConnectedOverlayPositions]="positions"
      [cdkConnectedOverlayScrollStrategy]="scrollStrategy"
      (backdropClick)="close()"
      (overlayKeydown)="onOverlayKeydown($event)"
      cdkConnectedOverlay
      cdkConnectedOverlayBackdropClass="cdk-overlay-transparent-backdrop"
    >
      <div [class]="panelClasses()" role="menu">
        @for (item of items(); track item.id) {
          <button
            [disabled]="item.disabled"
            [attr.aria-disabled]="item.disabled || null"
            [class]="itemClasses(item)"
            (click)="select(item)"
            type="button"
            role="menuitem"
          >
            @if (item.icon) {
              <cmn-icon [name]="item.icon" size="sm" aria-hidden="true" />
            }
            <span>{{ item.label }}</span>
          </button>
        }
      </div>
    </ng-template>
  `,
})
export class MenuComponent {
  private readonly overlay = inject(Overlay);

  public readonly items = input<MenuItem[]>([]);
  public readonly ariaLabel = input<string>('Open menu');
  public readonly triggerClass = input<string>('');
  public readonly panelClass = input<string>('');

  public readonly itemSelect = output<MenuItem>();

  public readonly positions = MENU_POSITIONS;
  public readonly isOpen = signal(false);
  public readonly triggerClasses = computed(() =>
    this.joinClasses(TRIGGER_BASE_CLASSES, this.triggerClass())
  );
  public readonly panelClasses = computed(() =>
    this.joinClasses(PANEL_BASE_CLASSES, this.panelClass())
  );
  public readonly scrollStrategy = this.overlay.scrollStrategies.reposition();

  public toggle(): void {
    this.isOpen.update(open => !open);
  }

  public close(): void {
    this.isOpen.set(false);
  }

  public select(item: MenuItem): void {
    if (item.disabled) {
      return;
    }
    this.close();
    this.itemSelect.emit(item);
  }

  public onOverlayKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.close();
    }
  }

  public itemClasses(item: MenuItem): string {
    const toneClasses = item.destructive ? ITEM_DESTRUCTIVE_CLASSES : ITEM_DEFAULT_CLASSES;
    return this.joinClasses(ITEM_BASE_CLASSES, toneClasses);
  }

  private joinClasses(...classes: (string | null | undefined)[]): string {
    return classes.filter(Boolean).join(' ');
  }
}
