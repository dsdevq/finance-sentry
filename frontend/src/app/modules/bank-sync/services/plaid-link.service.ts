import {Injectable, signal} from '@angular/core';
import {defer, from, Observable, of, switchMap} from 'rxjs';

export interface PlaidSuccessMetadata {
  institution: {name: string; institution_id: string} | null;
  accounts: {id: string; name: string; mask: string; type: string; subtype: string}[];
  link_session_id: string;
}

export interface PlaidLinkOptions {
  token: string;
  onSuccess: (publicToken: string, metadata: PlaidSuccessMetadata) => void;
  onExit?: (err: unknown, metadata: unknown) => void;
  onLoad?: () => void;
  onEvent?: (eventName: string, metadata: unknown) => void;
}

export interface PlaidHandler {
  open: () => void;
  destroy: () => void;
}

export interface PreparePlaidOptions {
  token: string;
  onSuccess: (publicToken: string, metadata: PlaidSuccessMetadata) => void;
  onExit?: (err: unknown) => void;
}

declare global {
  interface Window {
    Plaid?: {
      create: (options: PlaidLinkOptions) => PlaidHandler;
    };
  }
}

const PLAID_SCRIPT_URL = 'https://cdn.plaid.com/link/v2/stable/link-initialize.js';

@Injectable({providedIn: 'root'})
export class PlaidLinkService {
  private readonly readySignal = signal(false);
  private scriptPromise: Promise<void> | null = null;
  private handler: PlaidHandler | null = null;

  public readonly ready = this.readySignal.asReadonly();

  public prepare(options: PreparePlaidOptions): Observable<void> {
    return defer(() => from(this.ensureScript())).pipe(
      switchMap(() => {
        this.destroy();
        this.handler = this.createHandler(options);
        this.readySignal.set(true);
        return of(void 0);
      })
    );
  }

  public open(): void {
    this.handler?.open();
  }

  public destroy(): void {
    this.handler?.destroy();
    this.handler = null;
    this.readySignal.set(false);
  }

  private ensureScript(): Promise<void> {
    if (window.Plaid) {
      return Promise.resolve();
    }
    if (this.scriptPromise) {
      return this.scriptPromise;
    }
    this.scriptPromise = new Promise<void>((resolve, reject) => {
      const script = document.createElement('script');
      script.src = PLAID_SCRIPT_URL;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => {
        this.scriptPromise = null;
        reject(new Error('Failed to load Plaid Link script'));
      };
      document.head.appendChild(script);
    });
    return this.scriptPromise;
  }

  private createHandler(options: PreparePlaidOptions): PlaidHandler {
    if (!window.Plaid) {
      throw new Error('Plaid Link script is not loaded');
    }
    return window.Plaid.create({
      token: options.token,
      onSuccess: options.onSuccess,
      onExit: options.onExit ? (err): void => options.onExit?.(err) : undefined,
    });
  }
}
