import {ChangeDetectionStrategy, Component} from '@angular/core';
import {RouterLink, RouterLinkActive, RouterOutlet} from '@angular/router';
import {PageHeaderComponent} from '@lifekit-hq/ui';

import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';

interface AccountsTab {
  label: string;
  route: string;
}

@Component({
  selector: 'fns-accounts-shell',
  imports: [PageHeaderComponent, RouterLink, RouterLinkActive, RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto max-w-[1200px] p-cmn-8">
      <cmn-page-header
        title="Accounts"
        subtitle="Balances, holdings and net worth across every connected provider"
      />

      <nav class="mt-cmn-6 mb-cmn-8 flex gap-cmn-2 border-b border-border-default" role="tablist">
        @for (tab of tabs; track tab.route) {
          <a
            [routerLink]="tab.route"
            [routerLinkActiveOptions]="{exact: false}"
            routerLinkActive="text-text-primary border-accent-default"
            class="border-b-2 border-transparent px-cmn-4 py-cmn-2 text-cmn-sm font-medium text-text-secondary transition-colors hover:text-text-primary"
            role="tab"
          >
            {{ tab.label }}
          </a>
        }
      </nav>

      <router-outlet />
    </div>
  `,
})
export class AccountsShellComponent {
  public readonly tabs: AccountsTab[] = [
    {label: 'Inventory', route: AppRoute.AccountsList},
    {label: 'Investments', route: AppRoute.AccountsInvestments},
  ];
}
