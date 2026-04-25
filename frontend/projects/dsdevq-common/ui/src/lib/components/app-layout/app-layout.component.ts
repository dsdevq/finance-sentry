import {ChangeDetectionStrategy, Component, input, output} from '@angular/core';

import {type NavItem, SidebarNavComponent} from '../sidebar-nav/sidebar-nav.component';
import {TopBarComponent} from '../top-bar/top-bar.component';

export {type NavItem} from '../sidebar-nav/sidebar-nav.component';

@Component({
  selector: 'cmn-app-layout',
  imports: [SidebarNavComponent, TopBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex h-screen overflow-hidden bg-surface-bg">
      <cmn-sidebar-nav
        [items]="navItems()"
        [activeRoute]="activeRoute()"
        (navClick)="navClick.emit($event)"
        (collapsedChange)="collapsedChange.emit($event)"
      />
      <div class="flex flex-1 flex-col overflow-hidden">
        <cmn-top-bar
          [title]="title()"
          [isDark]="isDark()"
          [avatarLabel]="avatarLabel()"
          (searchClick)="searchClick.emit()"
          (themeToggle)="themeToggle.emit()"
          (avatarClick)="avatarClick.emit()"
        />
        <main class="flex-1 overflow-y-auto">
          <ng-content />
        </main>
      </div>
    </div>
  `,
})
export class AppLayoutComponent {
  public readonly navItems = input<NavItem[]>([]);
  public readonly activeRoute = input<string>('');
  public readonly title = input<string>('');
  public readonly isDark = input<boolean>(false);
  public readonly avatarLabel = input<string>('');

  public readonly navClick = output<NavItem>();
  public readonly collapsedChange = output<boolean>();
  public readonly searchClick = output<void>();
  public readonly themeToggle = output<void>();
  public readonly avatarClick = output<void>();
}
