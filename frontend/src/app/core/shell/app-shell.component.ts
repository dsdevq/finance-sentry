import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {toSignal} from '@angular/core/rxjs-interop';
import {NavigationEnd, Router, RouterOutlet} from '@angular/router';
import {
  AppLayoutComponent,
  CmnDialogBareContainerComponent,
  CmnDialogService,
  CommandPaletteComponent,
  type CommandPaletteItem,
  type NavItem,
  type PaletteResult,
  ThemeService,
} from '@dsdevq-common/ui';
import {filter, map} from 'rxjs';

import {AuthStore} from '../../modules/auth/store/auth.store';
import {AppRoute} from '../../shared/enums/app-route/app-route.enum';

const NAV_ITEMS: NavItem[] = [
  {label: 'Dashboard', icon: 'LayoutDashboard', route: AppRoute.Dashboard},
  {label: 'Accounts', icon: 'Building2', route: AppRoute.AccountsList},
  {label: 'Transactions', icon: 'ArrowLeftRight', route: AppRoute.Transactions},
  {label: 'Holdings', icon: 'ChartPie', route: AppRoute.Holdings},
  {label: 'Budgets', icon: 'Zap', route: AppRoute.Budgets},
  {label: 'Subscriptions', icon: 'RefreshCw', route: AppRoute.Subscriptions},
  {label: 'Alerts', icon: 'Bell', route: AppRoute.Alerts},
  {label: 'Settings', icon: 'Settings2', route: AppRoute.Settings},
];

const PALETTE_ITEMS: CommandPaletteItem[] = [
  {id: AppRoute.Dashboard, label: 'Dashboard', icon: 'LayoutDashboard', group: 'Pages'},
  {id: AppRoute.AccountsList, label: 'Accounts', icon: 'Building2', group: 'Pages'},
  {id: AppRoute.Transactions, label: 'Transactions', icon: 'ArrowLeftRight', group: 'Pages'},
  {id: AppRoute.Holdings, label: 'Asset Allocation', icon: 'PieChart', group: 'Pages'},
  {id: AppRoute.Budgets, label: 'Budgets', icon: 'Zap', group: 'Pages'},
  {id: AppRoute.Subscriptions, label: 'Subscriptions', icon: 'RefreshCw', group: 'Pages'},
  {id: AppRoute.Settings, label: 'Settings', icon: 'Settings2', group: 'Pages'},
  {id: '_connect', label: 'Connect Account', icon: 'Link', group: 'Actions'},
  {id: '_theme', label: 'Toggle Dark Mode', icon: 'Moon', group: 'Actions'},
  {id: '_logout', label: 'Sign Out', icon: 'LogOut', group: 'Actions'},
];

@Component({
  selector: 'fns-app-shell',
  imports: [AppLayoutComponent, RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(window:keydown)': 'onGlobalKeyDown($event)',
  },
  template: `
    <cmn-app-layout
      [navItems]="navItems"
      [activeRoute]="activeRoute()"
      [isDark]="isDark()"
      (navClick)="navigate($event)"
      (themeToggle)="themeService.toggle()"
      (searchClick)="openPalette()"
      avatarLabel="D"
    >
      <router-outlet />
    </cmn-app-layout>
  `,

})
export class AppShellComponent {
  private readonly router = inject(Router);
  private readonly authStore = inject(AuthStore);
  private readonly dialog = inject(CmnDialogService);
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

  public onGlobalKeyDown(e: KeyboardEvent): void {
    if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
      e.preventDefault();
      this.openPalette();
    }
  }

  public navigate(item: NavItem): void {
    void this.router.navigateByUrl(item.route);
  }

  public openPalette(): void {
    this.dialog
      .open<PaletteResult>(CommandPaletteComponent, {
        data: PALETTE_ITEMS,
        container: CmnDialogBareContainerComponent,
        hasBackdrop: false,
        panelClass: [],
        autoFocus: false,
        disableClose: true,
      })
      .afterClosed()
      .subscribe(result => {
        if (!result) {
          return;
        }
        if (result.type === 'navigate') {
          void this.router.navigateByUrl(result.id);
        } else {
          this.handleAction(result.id);
        }
      });
  }

  private handleAction(id: string): void {
    if (id === '_theme') {
      this.themeService.toggle();
    } else if (id === '_logout') {
      this.authStore.logout();
    } else if (id === '_connect') {
      void this.router.navigateByUrl(AppRoute.AccountsList);
    }
  }
}
