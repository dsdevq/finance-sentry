import {ChangeDetectionStrategy, Component, effect, inject} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';

import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';
import {AuthStore} from '../../store/auth.store';

@Component({
  selector: 'fns-mcp-connect',
  standalone: true,
  template: `
    <section class="mcp-connect">
      <h1>Connecting MCP</h1>
      <p>{{ message }}</p>
    </section>
  `,
  styles: [
    `
      .mcp-connect {
        max-width: 36rem;
        margin: 6rem auto;
        padding: 1.5rem;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class McpConnectComponent {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected message = 'Preparing Finance Sentry MCP authorization...';

  public constructor() {
    effect(() => {
      const authorizeUrl = this.route.snapshot.queryParamMap.get('authorizeUrl');
      if (!authorizeUrl) {
        this.message = 'Missing MCP authorization URL.';
        return;
      }

      if (!this.authStore.isAuthenticated()) {
        void this.router.navigate([AppRoute.Login], {
          queryParams: { returnUrl: `${AppRoute.McpConnect}?authorizeUrl=${encodeURIComponent(authorizeUrl)}` },
        });
        return;
      }

      window.location.assign(authorizeUrl);
    });
  }
}
