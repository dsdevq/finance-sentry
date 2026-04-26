import {inject, Injectable, type Type} from '@angular/core';
import {map, type Observable} from 'rxjs';

import {type Provider} from '../../../shared/models/provider/provider.model';
import {BinanceFormComponent} from '../components/connect-modal/binance-form.component';
import {type ConnectBinanceRequest} from '../models/binance/binance.model';
import {BinanceService} from '../services/binance.service';
import {type ConnectOutcome, type ConnectStrategy} from './connect-strategy';

@Injectable({providedIn: 'root'})
export class BinanceConnectStrategy implements ConnectStrategy {
  private readonly binance = inject(BinanceService);

  public readonly slug: Provider = 'binance';
  public readonly formComponent: Type<unknown> = BinanceFormComponent;

  public submit(input: unknown): Observable<ConnectOutcome> {
    const payload = input as ConnectBinanceRequest;
    return this.binance.connect(payload).pipe(
      map(() => ({
        successCode: 'POLLING' as const,
        count: 1,
        institutionType: 'crypto' as const,
      }))
    );
  }
}
