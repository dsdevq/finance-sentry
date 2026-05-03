import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {
  AlertItemComponent,
  type AlertItemSeverity,
  ButtonComponent,
  CardComponent,
  IconComponent,
  type LucideIconName,
  ToastService,
} from '@dsdevq-common/ui';

import {type AlertFilter, type AlertSeverity, type AlertType} from '../../models/alert/alert.model';
import {AlertsStore} from '../../store/alerts/alerts.store';

function iconForType(type: AlertType): LucideIconName {
  switch (type) {
    case 'LowBalance':
      return 'TriangleAlert';
    case 'SyncFailure':
      return 'CircleAlert';
    case 'UnusualSpend':
      return 'Zap';
  }
}

function labelForType(type: AlertType): string {
  switch (type) {
    case 'LowBalance':
      return 'low balance';
    case 'SyncFailure':
      return 'sync error';
    case 'UnusualSpend':
      return 'unusual spend';
  }
}

function severityFor(severity: AlertSeverity): AlertItemSeverity {
  switch (severity) {
    case 'Error':
      return 'error';
    case 'Warning':
      return 'warning';
    case 'Info':
      return 'info';
  }
}

@Component({
  selector: 'fns-alerts',
  imports: [AlertItemComponent, ButtonComponent, CardComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './alerts.component.html',
})
export class AlertsComponent {
  private readonly toast = inject(ToastService);

  public readonly store = inject(AlertsStore);

  public readonly filtered = computed(() => {
    const f = this.store.filter();
    const all = this.store.alerts();
    if (f === 'unread') {
      return all.filter(a => !a.isRead);
    }
    if (f === 'error') {
      return all.filter(a => a.severity === 'Error');
    }
    if (f === 'warning') {
      return all.filter(a => a.severity === 'Warning');
    }
    if (f === 'info') {
      return all.filter(a => a.severity === 'Info');
    }
    return all;
  });

  public readonly filterOptions: {id: AlertFilter; label: () => string}[] = [
    {id: 'all', label: () => `All (${this.store.alerts().length})`},
    {id: 'unread', label: () => `Unread (${this.store.unreadCount()})`},
    {id: 'error', label: () => `Errors (${this.store.errorCount()})`},
    {id: 'warning', label: () => `Warnings (${this.store.warningCount()})`},
    {id: 'info', label: () => 'Info'},
  ];

  public iconFor(type: AlertType): LucideIconName {
    return iconForType(type);
  }

  public typeLabel(type: AlertType): string {
    return labelForType(type);
  }

  public severityFor(severity: AlertSeverity): AlertItemSeverity {
    return severityFor(severity);
  }

  public markAllRead(): void {
    this.store.markAllRead();
    this.toast.show('All alerts marked as read', 'success');
  }

  public dismiss(id: string): void {
    this.store.dismiss(id);
    this.toast.show('Alert dismissed', 'info');
  }
}
