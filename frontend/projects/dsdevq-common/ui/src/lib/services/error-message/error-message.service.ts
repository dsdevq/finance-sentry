import {inject, Injectable} from '@angular/core';

import {ERROR_MESSAGES} from '../../tokens/error-messages.token';

@Injectable({providedIn: 'root'})
export class ErrorMessageService {
  private readonly messages = inject(ERROR_MESSAGES);

  public resolve(code: string | null | undefined): string | null {
    if (!code) {
      return null;
    }
    return this.messages[code] ?? null;
  }
}
