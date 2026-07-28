import {InstitutionLogoUtils} from './institution-logo.utils';

describe('InstitutionLogoUtils', () => {
  describe('faviconUrl', () => {
    it('resolves a direct-connect provider code to a direct-200 favicon URL', () => {
      expect(InstitutionLogoUtils.faviconUrl('binance', 'Binance')).toBe(
        'https://icons.duckduckgo.com/ip3/binance.com.ico'
      );
    });

    it('resolves IBKR and Monobank provider codes', () => {
      expect(InstitutionLogoUtils.faviconUrl('ibkr', null)).toContain('interactivebrokers.com');
      expect(InstitutionLogoUtils.faviconUrl('monobank', null)).toContain('monobank.ua');
    });

    it('is case-insensitive on the provider code', () => {
      expect(InstitutionLogoUtils.faviconUrl('BINANCE', null)).toContain('binance.com');
    });

    it('resolves an aggregator-connected bank by name when the provider is generic', () => {
      expect(InstitutionLogoUtils.faviconUrl('truelayer', 'Revolut')).toContain('revolut.com');
    });

    it('matches bank names by case-insensitive substring', () => {
      expect(InstitutionLogoUtils.faviconUrl('truelayer', 'Allied Irish Banks (GB)')).toContain(
        'aib.ie'
      );
    });

    it('prefers the provider code over the name when both match', () => {
      // provider binance wins even though the name mentions revolut
      expect(InstitutionLogoUtils.faviconUrl('binance', 'revolut')).toContain('binance.com');
    });

    it('returns null for an unknown institution', () => {
      expect(InstitutionLogoUtils.faviconUrl('plaid', 'Some Local Credit Union')).toBeNull();
    });

    it('returns null when both provider and name are null', () => {
      expect(InstitutionLogoUtils.faviconUrl(null, null)).toBeNull();
    });

    it('returns null when both provider and name are blank', () => {
      expect(InstitutionLogoUtils.faviconUrl('   ', '  ')).toBeNull();
    });
  });
});
