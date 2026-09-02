import {DatePipe, DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {Router} from '@angular/router';
import {AlertComponent, CardComponent, TagComponent} from '@lifekit-hq/ui';

import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';
import {DossierStore} from '../../store/dossier.store';

@Component({
  selector: 'fns-asset-dossier',
  imports: [AlertComponent, CardComponent, DatePipe, DecimalPipe, TagComponent],
  templateUrl: './asset-dossier.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DossierStore],
})
export class AssetDossierComponent {
  private readonly router = inject(Router);
  public readonly store = inject(DossierStore);
  public readonly pnlPositiveClass = 'text-status-success';
  public readonly pnlNegativeClass = 'text-status-error';

  public goBack(): void {
    void this.router.navigate([AppRoute.AccountsInvestments]);
  }
}
