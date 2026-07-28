/**
 * Institution logo domain hints. We never store logo images ourselves — a
 * domain here is turned into a favicon URL by {@link InstitutionLogoUtils}.
 *
 * PROVIDER_DOMAINS: direct-connect providers where the provider code IS the
 * institution (Monobank, IBKR, Binance).
 *
 * BANK_NAME_DOMAINS: aggregator-connected banks (e.g. TrueLayer) where the
 * specific institution lives in the display name, not the provider code.
 * Matched by case-insensitive substring, so keep keywords lowercase.
 */
export const PROVIDER_DOMAINS: Readonly<Record<string, string>> = {
  monobank: 'monobank.ua',
  ibkr: 'interactivebrokers.com',
  binance: 'binance.com',
};

export const BANK_NAME_DOMAINS: readonly (readonly [string, string])[] = [
  ['revolut', 'revolut.com'],
  ['allied irish', 'aib.ie'],
  ['aib', 'aib.ie'],
  ['monzo', 'monzo.com'],
  ['wise', 'wise.com'],
  ['starling', 'starlingbank.com'],
];
