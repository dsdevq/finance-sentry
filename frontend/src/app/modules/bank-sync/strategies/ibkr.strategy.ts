import {inject, Injectable, type Type} from '@angular/core';
import {map, type Observable} from 'rxjs';

import {type Provider} from '../../../shared/models/provider/provider.model';
import {IbkrFormComponent} from '../components/connect-modal/ibkr-form.component';
import {IBKRService} from '../services/ibkr.service';
import {type ConnectOutcome, type ConnectStrategy} from './connect-strategy';

@Injectable({providedIn: 'root'})
export class IbkrConnectStrategy implements ConnectStrategy {
  private readonly ibkr = inject(IBKRService);

  public readonly slug: Provider = 'ibkr';
  public readonly formComponent: Type<unknown> = IbkrFormComponent;

  public submit(): Observable<ConnectOutcome> {
    return this.ibkr.connect().pipe(
      map(response => ({
        successCode: 'POLLING' as const,
        count: response.holdingsCount,
        institutionType: 'broker' as const,
      }))
    );
  }
}
