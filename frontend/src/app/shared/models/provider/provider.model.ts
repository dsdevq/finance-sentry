export type Provider = 'plaid' | 'monobank' | 'binance' | 'ibkr' | 'truelayer';

export type BankProvider = Extract<Provider, 'plaid' | 'monobank' | 'truelayer'>;

export type InstitutionType = 'bank' | 'crypto' | 'broker';

export type ProviderFormShape =
  'plaid-link' | 'token' | 'key-secret' | 'user-pass' | 'open-banking-picker';

export interface ProviderDescriptor {
  readonly slug: Provider;
  readonly displayName: string;
  readonly institutionType: InstitutionType;
  readonly description: string;
  readonly iconAsset: string;
  readonly formShape: ProviderFormShape;
  readonly helpUrl: Nullable<string>;
}
