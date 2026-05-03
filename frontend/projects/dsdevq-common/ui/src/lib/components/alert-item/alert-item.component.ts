import {ChangeDetectionStrategy, Component, computed, input, output} from '@angular/core';

import {TagComponent} from '../tag/tag.component';
import {IconComponent, type LucideIconName} from '../icon/icon.component';

export type AlertItemSeverity = 'error' | 'warning' | 'info';

const SEVERITY_COLOR: Record<AlertItemSeverity, string> = {
  error: 'var(--color-status-error)',
  warning: 'var(--color-status-warning)',
  info: 'var(--color-status-info)',
};

const SEVERITY_ICON: Record<AlertItemSeverity, LucideIconName> = {
  error: 'CircleAlert',
  warning: 'TriangleAlert',
  info: 'Info',
};

const MS_PER_MINUTE = 60_000;
const MINUTES_PER_HOUR = 60;
const HOURS_PER_DAY = 24;

function formatRelativeTime(value: Nullable<string | number | Date>): string {
  if (!value) {
    return '';
  }
  const ts = value instanceof Date ? value.getTime() : new Date(value).getTime();
  if (Number.isNaN(ts)) {
    return '';
  }
  const mins = Math.floor((Date.now() - ts) / MS_PER_MINUTE);
  if (mins < 1) {
    return 'just now';
  }
  if (mins < MINUTES_PER_HOUR) {
    return `${mins}m ago`;
  }
  const hrs = Math.floor(mins / MINUTES_PER_HOUR);
  if (hrs < HOURS_PER_DAY) {
    return `${hrs}h ago`;
  }
  return `${Math.floor(hrs / HOURS_PER_DAY)}d ago`;
}

@Component({
  selector: 'cmn-alert-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TagComponent, IconComponent],
  template: `
    <div
      [class.border-border-default]="isRead()"
      [class.bg-surface-card]="isRead()"
      [style.border-color]="isRead() ? '' : color() + '30'"
      [style.background]="isRead() ? '' : color() + '08'"
      (click)="onContainerClick()"
      class="relative flex cursor-pointer items-start gap-cmn-3 rounded-cmn-lg border p-cmn-4 transition-colors"
    >
      @if (!isRead()) {
        <span
          [style.background]="color()"
          class="absolute right-cmn-3 top-cmn-3 h-2 w-2 rounded-full"
        ></span>
      }

      <div
        [style.background]="color() + '18'"
        class="flex h-9 w-9 shrink-0 items-center justify-center rounded-cmn-md"
      >
        <cmn-icon [name]="resolvedIcon()" [style.color]="color()" size="sm" />
      </div>

      <div class="flex-1 min-w-0">
        <div class="mb-cmn-1 flex flex-wrap items-center gap-cmn-2">
          <span
            [class.text-text-primary]="isRead()"
            [style.color]="isRead() ? '' : color()"
            class="font-label text-cmn-sm font-semibold"
            >{{ title() }}</span
          >
          @if (badgeLabel()) {
            <cmn-tag [variant]="severity()">{{ badgeLabel() }}</cmn-tag>
          }
          @if (referenceLabel()) {
            <span class="rounded bg-surface-raised px-1.5 py-0.5 text-[11px] text-text-disabled">{{
              referenceLabel()
            }}</span>
          }
        </div>
        <p class="mb-cmn-2 text-cmn-xs leading-relaxed text-text-secondary">
          {{ message() }}
        </p>
        @if (relativeTime()) {
          <span class="text-[11px] text-text-disabled">{{ relativeTime() }}</span>
        }
      </div>

      @if (dismissible()) {
        <button
          (click)="onDismissClick($event)"
          type="button"
          aria-label="Dismiss"
          class="flex shrink-0 rounded p-1 text-text-disabled transition-colors hover:bg-surface-raised hover:text-text-primary"
        >
          <cmn-icon name="X" size="sm" />
        </button>
      }
    </div>
  `,
})
export class AlertItemComponent {
  public readonly title = input.required<string>();
  public readonly message = input<string>('');
  public readonly severity = input<AlertItemSeverity>('info');
  public readonly icon = input<Nullable<LucideIconName>>(null);
  public readonly badgeLabel = input<Nullable<string>>(null);
  public readonly referenceLabel = input<Nullable<string>>(null);
  public readonly timestamp = input<Nullable<string | number | Date>>(null);
  public readonly isRead = input<boolean>(false);
  public readonly dismissible = input<boolean>(true);

  public readonly read = output<void>();
  public readonly dismissed = output<void>();

  public readonly color = computed(() => SEVERITY_COLOR[this.severity()]);
  public readonly resolvedIcon = computed<LucideIconName>(
    () => this.icon() ?? SEVERITY_ICON[this.severity()]
  );
  public readonly relativeTime = computed(() => formatRelativeTime(this.timestamp()));

  public onContainerClick(): void {
    if (!this.isRead()) {
      this.read.emit();
    }
  }

  public onDismissClick(event: MouseEvent): void {
    event.stopPropagation();
    this.dismissed.emit();
  }
}
