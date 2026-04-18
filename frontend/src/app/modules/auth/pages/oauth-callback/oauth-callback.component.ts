import {ChangeDetectionStrategy, Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';

import {AuthService} from '../../services/auth.service';

@Component({
  selector: 'fns-oauth-callback',
  standalone: true,
  imports: [],
  templateUrl: './oauth-callback.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OAuthCallbackComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  public ngOnInit(): void {
    const params = this.route.snapshot.queryParams as Record<string, string>;
    const {token, userId, expiresAt, error} = params;

    if (error === 'cancelled') {
      void this.router.navigate(['/login'], {queryParams: {info: 'google_cancelled'}});
      return;
    }

    if (error) {
      void this.router.navigate(['/login'], {queryParams: {error: 'google_failed'}});
      return;
    }

    if (token) {
      this.authService.handleOAuthCallback(token, userId ?? '', expiresAt ?? '');
      return;
    }

    void this.router.navigate(['/login'], {queryParams: {error: 'google_failed'}});
  }
}
