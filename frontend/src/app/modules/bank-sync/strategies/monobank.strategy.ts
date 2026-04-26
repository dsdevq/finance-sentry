import {inject, Injectable, type Type} from '@angular/core';
import {map, type Observable} from 'rxjs';

import {type Provider} from '../../../shared/models/provider/provider.model';
import {MonobankFormComponent} from '../components/connect-modal/monobank-form.component';
import {BankSyncService} from '../services/bank-sync.service';
import {type ConnectOutcome, type ConnectStrategy} from './connect-strategy';

export interface MonobankConnectPayload {
  readonly token: string;
}

@Injectable({providedIn: 'root'})
export class MonobankConnectStrategy implements ConnectStrategy {
  private readonly bankSync = inject(BankSyncService);

  public readonly slug: Provider = 'monobank';
  public readonly formComponent: Type<unknown> = MonobankFormComponent;

  public submit(input: unknown): Observable<ConnectOutcome> {
    const payload = input as MonobankConnectPayload;
    return this.bankSync.connectMonobank(payload.token).pipe(
      map(response => ({
        successCode: 'POLLING' as const,
        count: response.accounts.length,
        institutionType: 'bank' as const,
      }))
    );
  }
}
