import {type ProviderDescriptor} from '../../models/provider/provider.model';

export const PROVIDER_CATALOG: readonly ProviderDescriptor[] = Object.freeze([
  Object.freeze({
    slug: 'plaid',
    displayName: 'Plaid',
    institutionType: 'bank',
    description: 'Connect US, Canadian, or European banks via Plaid Link.',
    iconAsset: '/assets/providers/plaid.svg',
    formShape: 'plaid-link',
    helpUrl: 'https://plaid.com/docs/link/',
  }),
  Object.freeze({
    slug: 'monobank',
    displayName: 'Monobank',
    institutionType: 'bank',
    description: 'Connect Monobank cards using a personal API token.',
    iconAsset: '/assets/providers/monobank.svg',
    formShape: 'token',
    helpUrl: 'https://api.monobank.ua',
  }),
  Object.freeze({
    slug: 'binance',
    displayName: 'Binance',
    institutionType: 'crypto',
    description: 'Connect a Binance account with a read-only API key and secret.',
    iconAsset: '/assets/providers/binance.svg',
    formShape: 'key-secret',
    helpUrl: 'https://www.binance.com/en/support/faq/360002502072',
  }),
  Object.freeze({
    slug: 'ibkr',
    displayName: 'Interactive Brokers',
    institutionType: 'broker',
    description: 'Connect an Interactive Brokers account via gateway credentials.',
    iconAsset: '/assets/providers/ibkr.svg',
    formShape: 'user-pass',
    helpUrl: 'https://www.interactivebrokers.com/en/trading/free-demo.php',
  }),
] as const);
