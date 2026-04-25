export type InstitutionType = 'bank' | 'crypto' | 'broker';

export type ModalStep =
  | 'type-picker'
  | 'bank-picker'
  | 'monobank-form'
  | 'binance-form'
  | 'ibkr-form'
  | 'closed';
