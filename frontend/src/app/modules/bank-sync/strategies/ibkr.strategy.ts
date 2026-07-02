import {inject, Injectable, type Type} from '@angular/core';
import {filter, type Observable, of, switchMap, take, throwError, timer} from 'rxjs';

import {type Provider} from '../../../shared/models/provider/provider.model';
import {IbkrFormComponent} from '../components/connect-modal/ibkr-form.component';
import {type ConnectIBKRRequest, type IBKRConnectSessionSnapshot} from '../models/ibkr/ibkr.model';
import {IBKRService} from '../services/ibkr.service';
import {type ConnectOutcome, type ConnectStrategy} from './connect-strategy';

const POLL_INTERVAL_MS = 1500;
const TERMINAL_STATES: ReadonlySet<IBKRConnectSessionSnapshot['status']> = new Set([
  'completed',
  'failed',
  'cancelled',
]);

@Injectable({providedIn: 'root'})
export class IbkrConnectStrategy implements ConnectStrategy {
  private readonly ibkr = inject(IBKRService);

  public readonly slug: Provider = 'ibkr';
  public readonly formComponent: Type<unknown> = IbkrFormComponent;

  public submit(input: unknown): Observable<ConnectOutcome> {
    const payload = input as ConnectIBKRRequest;
    return this.ibkr.createConnectSession(payload).pipe(
      switchMap(({sessionId}) => this.pollUntilTerminal(sessionId)),
      switchMap(snapshot => this.mapTerminal(snapshot))
    );
  }

  private pollUntilTerminal(sessionId: string): Observable<IBKRConnectSessionSnapshot> {
    return timer(0, POLL_INTERVAL_MS).pipe(
      switchMap(() => this.ibkr.getConnectStatus(sessionId)),
      filter(snapshot => TERMINAL_STATES.has(snapshot.status)),
      take(1)
    );
  }

  private mapTerminal(snapshot: IBKRConnectSessionSnapshot): Observable<ConnectOutcome> {
    if (snapshot.status === 'completed' && snapshot.result) {
      return of({
        successCode: 'CONNECTED' as const,
        count: snapshot.result.holdingsCount,
        institutionType: 'broker' as const,
      });
    }
    return throwError(() => ({
      error: {
        errorCode: snapshot.errorCode ?? 'INTERNAL_ERROR',
        error: snapshot.errorMessage ?? 'IBKR connect failed.',
      },
    }));
  }
}
