import {effect, inject, untracked} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {patchState, signalStoreFeature, withHooks} from '@ngrx/signals';

export interface UrlParamCodec<T> {
  encode: (value: T) => string;
  decode: (raw: string) => T;
}

export type BuiltinCodec = 'string' | 'number' | 'boolean';

export interface UrlParamSchema<T> {
  param: string;
  default: T;
  codec?: BuiltinCodec | UrlParamCodec<T>;
}

export type UrlSyncSchema<TState> = {
  [K in keyof TState]?: UrlParamSchema<TState[K]>;
};

const STRING_CODEC: UrlParamCodec<string> = {
  encode: v => v,
  decode: v => v,
};

const NUMBER_CODEC: UrlParamCodec<number> = {
  encode: v => String(v),
  decode: v => Number(v),
};

const BOOLEAN_CODEC: UrlParamCodec<boolean> = {
  encode: v => (v ? 'true' : 'false'),
  decode: v => v === 'true',
};

function resolveCodec<T>(codec: UrlParamSchema<T>['codec']): UrlParamCodec<T> {
  if (codec && typeof codec === 'object') {
    return codec;
  }
  switch (codec) {
    case 'number':
      return NUMBER_CODEC as unknown as UrlParamCodec<T>;
    case 'boolean':
      return BOOLEAN_CODEC as unknown as UrlParamCodec<T>;
    case 'string':
    case undefined:
      return STRING_CODEC as unknown as UrlParamCodec<T>;
  }
}

function valuesEqual<T>(a: T, b: T): boolean {
  if (a === b) {
    return true;
  }
  if (a === null || b === null || typeof a !== 'object' || typeof b !== 'object') {
    return false;
  }
  return JSON.stringify(a) === JSON.stringify(b);
}

export function withUrlSync<TState extends object>(schema: UrlSyncSchema<TState>) {
  const entries = Object.entries(schema) as [keyof TState & string, UrlParamSchema<unknown>][];

  return signalStoreFeature(
    withHooks({
      onInit(store) {
        const route = inject(ActivatedRoute);
        const router = inject(Router);

        const params = route.snapshot.queryParamMap;
        const hydrated: Record<string, unknown> = {};
        for (const [field, cfg] of entries) {
          const raw = params.get(cfg.param);
          if (raw !== null) {
            hydrated[field] = resolveCodec(cfg.codec).decode(raw);
          }
        }
        if (Object.keys(hydrated).length > 0) {
          patchState(store, hydrated as Partial<object>);
        }

        let firstRun = true;
        effect(() => {
          const queryParams: Record<string, string | null> = {};
          for (const [field, cfg] of entries) {
            const value = (store as unknown as Record<string, () => unknown>)[field]();
            queryParams[cfg.param] = valuesEqual(value, cfg.default)
              ? null
              : resolveCodec(cfg.codec).encode(value);
          }
          if (firstRun) {
            firstRun = false;
            return;
          }
          untracked(() => {
            void router.navigate([], {
              queryParams,
              queryParamsHandling: 'merge',
              replaceUrl: true,
              relativeTo: route,
            });
          });
        });
      },
    })
  );
}
