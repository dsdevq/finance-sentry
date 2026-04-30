export interface SortParam {
  field: string;
  direction: 'asc' | 'desc';
}

export interface FilterParam {
  field: string;
  op: 'eq' | 'gt' | 'lt' | 'contains';
  value: string;
}

export interface PagedRequest {
  offset?: number;
  limit?: number;
  sort?: SortParam[];
  filters?: FilterParam[];
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  offset: number;
  limit: number;
  hasMore: boolean;
}

export interface ApiError {
  error: string;
  errorCode: string;
  details?: string[];
}
