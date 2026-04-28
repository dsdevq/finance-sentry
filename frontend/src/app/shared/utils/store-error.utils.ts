import {catchError, EMPTY, type MonoTypeOperatorFunction} from 'rxjs';

import {ErrorUtils} from './error.utils';

interface ErrorSink {
  setError(code: Nullable<string>): void;
}

export class StoreErrorUtils {
  public static catchAndSetError<T>(sink: ErrorSink): MonoTypeOperatorFunction<T> {
    return catchError<T, typeof EMPTY>((err: unknown) => {
      sink.setError(ErrorUtils.extractCode(err));
      return EMPTY;
    });
  }
}
