import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {toSignal} from '@angular/core/rxjs-interop';
import {NavigationEnd, Router, RouterOutlet} from '@angular/router';
import {AppLayoutComponent, type NavItem, ThemeService} from '@dsdevq-common/ui';
import {filter, map} from 'rxjs';

import {AppRoute} from '../../shared/enums/app-route/app-route.enum';

const NAV_ITEMS: NavItem[] = [
  {label: 'Dashboard', icon: 'LayoutDashboard', route: AppRoute.Dashboard},
  {label: 'Accounts', icon: 'Building2', route: AppRoute.AccountsList},
  {label: 'Transactions', icon: 'ArrowLeftRight', route: AppRoute.Transactions},
  {label: 'Holdings', icon: 'ChartPie', route: AppRoute.Holdings},
];

@Component({
  selector: 'fns-app-shell',
  imports: [AppLayoutComponent, RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <cmn-app-layout
      [navItems]="navItems"
      [activeRoute]="activeRoute()"
      [isDark]="isDark()"
      (navClick)="navigate($event)"
      (themeToggle)="themeService.toggle()"
      avatarLabel="D"
    >
      <router-outlet />
    </cmn-app-layout>
  `,
})
export class AppShellComponent {
  private readonly router = inject(Router);
  private readonly theme = toSignal(inject(ThemeService).activeTheme$, {
    initialValue: 'light' as const,
  });
  private readonly routerUrl = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects)
    ),
    {initialValue: this.router.url}
  );

  public readonly themeService = inject(ThemeService);
  public readonly navItems = NAV_ITEMS;
  public readonly isDark = computed(() => this.theme() === 'dark');
  public readonly activeRoute = computed(() => {
    const url = this.routerUrl();
    const match = NAV_ITEMS.find(item => url.startsWith(item.route));
    return match?.route ?? '';
  });

  public navigate(item: NavItem): void {
    void this.router.navigateByUrl(item.route);
  }
}
