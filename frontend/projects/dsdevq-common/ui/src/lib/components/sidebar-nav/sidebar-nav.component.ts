import {NgClass} from '@angular/common';
import {ChangeDetectionStrategy, Component, input, output, signal} from '@angular/core';

import {BadgeComponent} from '../badge/badge.component';
import {IconComponent, LucideIconName} from '../icon/icon.component';

export interface NavItem {
  label: string;
  icon: LucideIconName;
  route: string;
  badge?: () => number;
}

@Component({
  selector: 'cmn-sidebar-nav',
  imports: [NgClass, IconComponent, BadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside
      [ngClass]="[
        'flex flex-col h-full border-r border-border-default bg-surface-card transition-[width] duration-200',
        collapsed() ? 'w-16' : 'w-60',
      ]"
    >
      <!-- Logo / toggle -->
      <div class="flex h-14 items-center justify-between px-cmn-4 border-b border-border-default">
        @if (!collapsed()) {
          <span class="font-headline text-cmn-sm font-semibold text-text-primary tracking-tight">
            Finance Sentry
          </span>
        }
        <button
          [title]="collapsed() ? 'Expand sidebar' : 'Collapse sidebar'"
          (click)="toggleCollapsed()"
          class="ml-auto flex h-8 w-8 items-center justify-center rounded-cmn-md text-text-secondary hover:bg-surface-raised hover:text-text-primary transition-colors"
        >
          <cmn-icon [name]="collapsed() ? 'PanelLeftOpen' : 'PanelLeftClose'" size="sm" />
        </button>
      </div>

      <!-- Nav items -->
      <nav class="flex flex-col gap-cmn-1 p-cmn-2 flex-1">
        @for (item of items(); track item.route) {
          <button
            [ngClass]="navItemClass(item)"
            [title]="collapsed() ? item.label : ''"
            (click)="navClick.emit(item)"
          >
            <cmn-badge [count]="item.badge ? item.badge() : 0" status="error">
              <cmn-icon [name]="item.icon" size="md" />
            </cmn-badge>
            @if (!collapsed()) {
              <span class="truncate font-label text-cmn-sm font-medium">{{ item.label }}</span>
            }
          </button>
        }
      </nav>
    </aside>
  `,
})
export class SidebarNavComponent {
  public readonly items = input<NavItem[]>([]);
  public readonly activeRoute = input<string>('');

  public readonly navClick = output<NavItem>();
  public readonly collapsedChange = output<boolean>();

  public readonly collapsed = signal<boolean>(false);

  public toggleCollapsed(): void {
    const next = !this.collapsed();
    this.collapsed.set(next);
    this.collapsedChange.emit(next);
  }

  public navItemClass(item: NavItem): string {
    const isActive = this.activeRoute() === item.route;
    const base =
      'flex items-center gap-cmn-3 rounded-cmn-md px-cmn-3 py-cmn-2 transition-colors w-full text-left';
    const active = 'bg-accent-subtle text-accent-default font-semibold';
    const inactive = 'text-text-secondary hover:bg-surface-raised hover:text-text-primary';
    return `${base} ${isActive ? active : inactive}`;
  }
}
