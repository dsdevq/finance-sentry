import {inject, Injectable, type Type} from '@angular/core';
import {map, type Observable} from 'rxjs';

import {type Provider} from '../../../shared/models/provider/provider.model';
import {TrueLayerPickerComponent} from '../components/connect-modal/truelayer-picker.component';
import {BankSyncService} from '../services/bank-sync.service';
import {type ConnectOutcome, type ConnectStrategy} from './connect-strategy';

export interface TrueLayerConnectPayload {
  readonly providerId: string;
  readonly providerName: string;
}

@Injectable({providedIn: 'root'})
export class TrueLayerConnectStrategy implements ConnectStrategy {
  private readonly bankSync = inject(BankSyncService);

  public readonly slug: Provider = 'truelayer';
  public readonly formComponent: Type<unknown> = TrueLayerPickerComponent;

  public submit(input: unknown): Observable<ConnectOutcome> {
    const payload = input as TrueLayerConnectPayload;
    return this.bankSync
      .beginTrueLayerConnect({
        providerId: payload.providerId,
        providerName: payload.providerName,
      })
      .pipe(
        map(response => {
          // Redirect to TrueLayer consent flow; the backend callback bounces the
          // user back to /accounts/list once authorization is complete.
          window.location.assign(response.link);
          return {
            successCode: 'POLLING' as const,
            count: 0,
            institutionType: 'bank' as const,
          };
        })
      );
  }
}
