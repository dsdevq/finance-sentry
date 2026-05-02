import {type Observable, timer} from 'rxjs';
import {shareReplay, switchMap, takeWhile} from 'rxjs/operators';

export interface PollOptions<T> {
  intervalMs: number;
  isDone: (value: T) => boolean;
  startImmediately?: boolean;
}

export function poll<T>(request: () => Observable<T>, options: PollOptions<T>): Observable<T> {
  const startDelay = options.startImmediately === false ? options.intervalMs : 0;
  return timer(startDelay, options.intervalMs).pipe(
    switchMap(() => request()),
    takeWhile(value => !options.isDone(value), true),
    shareReplay(1)
  );
}
