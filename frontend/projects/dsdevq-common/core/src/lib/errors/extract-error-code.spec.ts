import {HttpErrorResponse} from '@angular/common/http';
import {describe, expect, it} from 'vitest';

import {extractErrorCode} from './extract-error-code';

describe('extractErrorCode', () => {
  it('should return the errorCode from an HttpErrorResponse body', () => {
    const err = new HttpErrorResponse({error: {errorCode: 'invalid_credentials'}, status: 401});
    expect(extractErrorCode(err)).toBe('invalid_credentials');
  });

  it('should return null when the body has no errorCode', () => {
    const err = new HttpErrorResponse({error: {message: 'boom'}, status: 500});
    expect(extractErrorCode(err)).toBeNull();
  });

  it('should return null when errorCode is not a string', () => {
    const err = new HttpErrorResponse({error: {errorCode: 42}, status: 422});
    expect(extractErrorCode(err)).toBeNull();
  });

  it('should return null when the error body is a string', () => {
    const err = new HttpErrorResponse({error: 'plain text', status: 502});
    expect(extractErrorCode(err)).toBeNull();
  });

  it('should return null for a null body', () => {
    const err = new HttpErrorResponse({error: null, status: 0});
    expect(extractErrorCode(err)).toBeNull();
  });

  it('should return null for non-HttpErrorResponse values', () => {
    expect(extractErrorCode(new Error('boom'))).toBeNull();
    expect(extractErrorCode(undefined)).toBeNull();
    expect(extractErrorCode({errorCode: 'not_an_http_error'})).toBeNull();
  });
});
