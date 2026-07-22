import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {Router} from '@angular/router';
import {
  AlertItemComponent,
  type AlertItemSeverity,
  ChipComponent,
  EmptyStateComponent,
  type LucideIconName,
  PageHeaderComponent,
  ToastService,
} from '@dsdevq-common/ui';

import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';
import {ALERT_TYPE_META_REGISTRY} from '../../constants/alert-type-meta.constants';
import {
  type Alert,
  type AlertFilter,
  type AlertSeverity,
  type AlertType,
} from '../../models/alert/alert.model';
import {AlertsStore} from '../../store/alerts/alerts.store';

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
  imports: [AlertItemComponent, ChipComponent, EmptyStateComponent, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './alerts.component.html',
})
export class AlertsComponent {
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  public readonly store = inject(AlertsStore);

  public readonly alertsSubtitle = computed(() => {
    const count = this.store.unreadCount();
    if (count > 0) {
      return `${count} unread ${count === 1 ? 'alert' : 'alerts'}`;
    }
    return 'All caught up';
  });

  public readonly emptyTitle = computed(() =>
    this.store.filter() === 'all' ? 'No alerts' : `No ${this.store.filter()} alerts`
  );

  public readonly emptySubtext = computed(() =>
    this.store.filter() === 'all' ? 'All your accounts are healthy.' : 'Try a different filter.'
  );

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
    return ALERT_TYPE_META_REGISTRY[type].icon;
  }

  public typeLabel(type: AlertType): string {
    return ALERT_TYPE_META_REGISTRY[type].label;
  }

  public typeDescription(type: AlertType): string {
    return ALERT_TYPE_META_REGISTRY[type].description;
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

  public openAlert(alert: Alert): void {
    if (!alert.isRead) {
      this.store.markRead(alert.id);
    }
    const target = alert.type === 'UnusualSpend' ? AppRoute.Transactions : AppRoute.AccountsList;
    void this.router.navigateByUrl(target);
  }
}
