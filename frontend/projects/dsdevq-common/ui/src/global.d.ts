declare global {
  type Nullable<T> = T | null;
  type Maybe<T> = Nullable<T> | undefined;

  type AsyncStatus = 'idle' | 'loading' | 'error';

  interface ApiErrorResponse {
    error?: {
      errorCode?: string;
      message?: string;
    };
  }

  interface OffsetPagination {
    offset: number;
    limit: number;
    totalCount: number;
    hasMore: boolean;
  }

  interface OffsetPaginationParams {
    offset?: number;
    limit?: number;
  }
}

export {};
