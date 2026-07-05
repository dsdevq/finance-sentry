import {ChangeDetectionStrategy, Component, effect, inject} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';

import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';
import {environment} from '../../../../../environments/environment';
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
      const redirectUri = this.route.snapshot.queryParamMap.get('redirectUri');
      const state = this.route.snapshot.queryParamMap.get('state');
      if (!redirectUri || !state) {
        this.message = 'Missing MCP authorization parameters.';
        return;
      }

      if (!this.authStore.isAuthenticated()) {
        const returnUrl = this.router.createUrlTree([AppRoute.McpConnect], {
          queryParams: {redirectUri, state},
        }).toString();
        void this.router.navigate([AppRoute.Login], {
          queryParams: {returnUrl},
        });
        return;
      }

      const authorizeUrl = new URL(`${environment.apiBaseUrl}/auth/mcp/authorize`);
      authorizeUrl.searchParams.set('redirectUri', redirectUri);
      authorizeUrl.searchParams.set('state', state);
      window.location.assign(authorizeUrl.toString());
    });
  }
}
