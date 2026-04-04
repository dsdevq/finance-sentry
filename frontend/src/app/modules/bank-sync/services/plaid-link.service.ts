import {Injectable} from '@angular/core';

export interface PlaidLinkOptions {
  token: string;
  onSuccess: (publicToken: string, metadata: unknown) => void;
  onExit?: (err: unknown, metadata: unknown) => void;
  onLoad?: () => void;
  onEvent?: (eventName: string, metadata: unknown) => void;
}

export interface PlaidHandler {
  open: () => void;
  destroy: () => void;
}

// Plaid Link is loaded at runtime via CDN script tag.
// The global `window.Plaid` object is injected by the script.
declare global {
  interface Window {
    Plaid?: {
      create: (options: PlaidLinkOptions) => PlaidHandler;
    };
  }
}

@Injectable({providedIn: 'root'})
export class PlaidLinkService {
  private scriptLoaded = false;
  private readonly plaidScriptUrl = 'https://cdn.plaid.com/link/v2/stable/link-initialize.js';

  public loadScript(): Promise<void> {
    if (this.scriptLoaded || window.Plaid) {
      this.scriptLoaded = true;
      return Promise.resolve();
    }

    return new Promise<void>((resolve, reject) => {
      const script = document.createElement('script');
      script.src = this.plaidScriptUrl;
      script.async = true;
      script.onload = () => {
        this.scriptLoaded = true;
        resolve();
      };
      script.onerror = () => reject(new Error('Failed to load Plaid Link script'));
      document.head.appendChild(script);
    });
  }

  public create(options: PlaidLinkOptions): PlaidHandler {
    if (!window.Plaid) {
      throw new Error('Plaid Link script is not loaded');
    }
    return window.Plaid.create(options);
  }
}
