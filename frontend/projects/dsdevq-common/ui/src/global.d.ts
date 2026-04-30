declare global {
  type Nullable<T> = T | null;
  type Maybe<T> = Nullable<T> | undefined;

  type AsyncStatus = 'idle' | 'loading' | 'error';
}

export {};
