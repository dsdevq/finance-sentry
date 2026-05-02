import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  IconComponent,
  ToastService,
} from '@dsdevq-common/ui';

import {type AlertFilter, type AlertType} from '../../models/alert/alert.model';
import {AlertsStore} from '../../store/alerts/alerts.store';

const MS_PER_MINUTE = 60_000;
const MINUTES_PER_HOUR = 60;
const HOURS_PER_DAY = 24;

function iconForType(type: AlertType): string {
  switch (type) {
    case 'sync_error':
      return 'AlertCircle';
    case 'low_balance':
      return 'AlertTriangle';
    case 'unusual_spend':
      return 'Zap';
    case 'budget':
      return 'TrendingDown';
    default:
      return 'Info';
  }
}

function colorForSeverity(severity: string): string {
  switch (severity) {
    case 'error':
      return 'var(--color-status-error)';
    case 'warning':
      return 'var(--color-status-warning)';
    default:
      return 'var(--color-status-info)';
  }
}

@Component({
  selector: 'fns-alerts',
  imports: [BadgeComponent, ButtonComponent, CardComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AlertsStore],
  templateUrl: './alerts.component.html',
})
export class AlertsComponent {
  private readonly toast = inject(ToastService);

  public readonly store = inject(AlertsStore);

  public readonly filterOptions: {id: AlertFilter; label: () => string}[] = [
    {id: 'all', label: () => `All (${this.store.alerts().length})`},
    {id: 'unread', label: () => `Unread (${this.store.unreadCount()})`},
    {id: 'error', label: () => `Errors (${this.store.errorCount()})`},
    {id: 'warning', label: () => `Warnings (${this.store.warningCount()})`},
    {id: 'info', label: () => 'Info'},
  ];

  public iconFor(type: AlertType): string {
    return iconForType(type);
  }

  public colorFor(severity: string): string {
    return colorForSeverity(severity);
  }

  public relativeTime(ts: number): string {
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

  public markAllRead(): void {
    this.store.markAllRead();
    this.toast.show('All alerts marked as read', 'success');
  }

  public clearAll(): void {
    this.store.clearAll();
    this.toast.show('All alerts cleared', 'info');
  }

  public dismiss(id: string): void {
    this.store.dismiss(id);
    this.toast.show('Alert dismissed', 'info');
  }
}
