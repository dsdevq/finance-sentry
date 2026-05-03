import {describe, expect, it} from 'vitest';

import {SubscriptionUtils} from './subscription.utils';

describe('SubscriptionUtils.getMerchantColor', () => {
  it('returns a valid hsl string for a normal name', () => {
    const color = SubscriptionUtils.getMerchantColor('Netflix');
    expect(color).toMatch(/^hsl\(\d+, 55%, 42%\)$/);
  });

  it('returns the same color for the same name (deterministic)', () => {
    expect(SubscriptionUtils.getMerchantColor('Spotify')).toBe(
      SubscriptionUtils.getMerchantColor('Spotify')
    );
  });

  it('returns different colors for different names', () => {
    expect(SubscriptionUtils.getMerchantColor('Netflix')).not.toBe(
      SubscriptionUtils.getMerchantColor('Spotify')
    );
  });

  it('returns fallback color for empty string', () => {
    expect(SubscriptionUtils.getMerchantColor('')).toBe('hsl(220, 14%, 50%)');
  });
});
