export class ErrorUtils {
  public static extractCode(err: unknown): Nullable<string> {
    return (err as Maybe<ApiErrorResponse>)?.error?.errorCode ?? null;
  }
}
