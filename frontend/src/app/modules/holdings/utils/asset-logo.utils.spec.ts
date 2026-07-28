import {environment} from '../../../../environments/environment';
import {AssetLogoUtils} from './asset-logo.utils';

describe('AssetLogoUtils', () => {
  describe('logoUrl', () => {
    it('builds a CoinCap icon URL for a Binance crypto asset (lowercased)', () => {
      expect(AssetLogoUtils.logoUrl('SOL', 'binance')).toBe(
        'https://assets.coincap.io/assets/icons/sol@2x.png'
      );
    });

    it('is provider-case-insensitive', () => {
      expect(AssetLogoUtils.logoUrl('XRP', 'Binance')).toContain('/xrp@2x.png');
    });

    it('returns null for an IBKR ticker when no logo.dev token is configured', () => {
      // Default environment ships with an empty token.
      expect(environment.logoDevToken).toBe('');
      expect(AssetLogoUtils.logoUrl('AAPL', 'ibkr')).toBeNull();
    });

    it('builds a logo.dev URL for an IBKR ticker when a token is set', () => {
      const original = environment.logoDevToken;
      try {
        (environment as {logoDevToken: string}).logoDevToken = 'pk_test';
        const url = AssetLogoUtils.logoUrl('aapl', 'ibkr');
        expect(url).toContain('https://img.logo.dev/ticker/AAPL');
        expect(url).toContain('token=pk_test');
      } finally {
        (environment as {logoDevToken: string}).logoDevToken = original;
      }
    });

    it('returns null for an unknown provider', () => {
      expect(AssetLogoUtils.logoUrl('AAPL', 'monobank')).toBeNull();
    });

    it('returns null for a blank symbol', () => {
      expect(AssetLogoUtils.logoUrl('  ', 'binance')).toBeNull();
    });

    it('returns null for a null symbol', () => {
      expect(AssetLogoUtils.logoUrl(null, 'binance')).toBeNull();
    });
  });
});
