import {inject, Injectable, type Type} from '@angular/core';
import {catchError, EMPTY, Observable, Subject, switchMap, take, tap, throwError} from 'rxjs';

import {type Provider} from '../../../shared/models/provider/provider.model';
import {ErrorUtils} from '../../../shared/utils/error.utils';
import {PlaidLauncherComponent} from '../components/connect-modal/plaid-launcher.component';
import {type PlaidSuccessMetadata} from '../models/plaid/plaid.model';
import {BankSyncService} from '../services/bank-sync.service';
import {PlaidLinkService} from '../services/plaid-link.service';
import {type ConnectOutcome, type ConnectStrategy} from './connect-strategy';

interface PlaidSubmissionError extends Error {
  errorCode?: string;
}

@Injectable({providedIn: 'root'})
export class PlaidConnectStrategy implements ConnectStrategy {
  private readonly bankSync = inject(BankSyncService);
  private readonly plaidLink = inject(PlaidLinkService);

  public readonly slug: Provider = 'plaid';
  public readonly formComponent: Type<unknown> = PlaidLauncherComponent;

  public submit(): Observable<ConnectOutcome> {
    return new Observable<ConnectOutcome>(subscriber => {
      const settled$ = new Subject<ConnectOutcome>();
      const settledSub = settled$.subscribe(subscriber);

      const sub = this.bankSync
        .getLinkToken()
        .pipe(
          catchError((err: unknown) => throwError(() => this.toScriptLoadError(err))),
          switchMap(linkResponse =>
            this.plaidLink
              .prepare({
                token: linkResponse.linkToken,
                onSuccess: (publicToken: string, metadata: PlaidSuccessMetadata): void => {
                  this.exchange(publicToken, metadata, settled$);
                },
              })
              .pipe(
                catchError((err: unknown) => throwError(() => this.toScriptLoadError(err))),
                tap(() => this.plaidLink.open())
              )
          )
        )
        .subscribe({
          error: err => subscriber.error(err),
        });

      return (): void => {
        sub.unsubscribe();
        settledSub.unsubscribe();
        this.plaidLink.destroy();
      };
    });
  }

  private exchange(
    publicToken: string,
    metadata: PlaidSuccessMetadata,
    sink: Subject<ConnectOutcome>
  ): void {
    const institutionName = metadata.institution?.name ?? 'Unknown';
    this.bankSync
      .exchangePublicToken(publicToken, institutionName)
      .pipe(
        take(1),
        catchError((err: unknown) => {
          const code = ErrorUtils.extractCode(err) ?? 'PLAID_LINK_FAILED';
          sink.error(this.makeError(code, err));
          return EMPTY;
        })
      )
      .subscribe(() => {
        sink.next({successCode: 'POLLING', count: 1, institutionType: 'bank'});
        sink.complete();
      });
  }

  private toScriptLoadError(err: unknown): PlaidSubmissionError {
    const code = ErrorUtils.extractCode(err) ?? 'PLAID_SCRIPT_LOAD_FAILED';
    return this.makeError(code, err);
  }

  private makeError(code: string, cause: unknown): PlaidSubmissionError {
    const e = new Error(typeof cause === 'string' ? cause : code) as PlaidSubmissionError;
    e.errorCode = code;
    return e;
  }
}
