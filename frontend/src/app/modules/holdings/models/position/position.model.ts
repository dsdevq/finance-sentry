export interface BrokeragePositionDto {
  symbol: string;
  instrumentType: string;
  quantity: number;
  usdValue: number;
  costBasisUsd: Nullable<number>;
  averageCostUsd: Nullable<number>;
}

export interface BrokerageHoldingsDto {
  provider: string;
  syncedAt: Nullable<string>;
  isStale: boolean;
  positions: BrokeragePositionDto[];
  totalUsdValue: number;
}

export interface CryptoHoldingDto {
  asset: string;
  freeQuantity: number;
  lockedQuantity: number;
  usdValue: number;
}

export interface CryptoHoldingsDto {
  provider: string;
  syncedAt: Nullable<string>;
  isStale: boolean;
  holdings: CryptoHoldingDto[];
  totalUsdValue: number;
}

export interface Position {
  symbol: string;
  provider: string;
  quantity: number;
  currentValue: number;
  currentPrice: number;
  // Provider-supplied P&L only (IBKR cost basis). Null when the provider gives
  // us no cost basis (crypto cost basis is reconstructed on our side, so we do
  // not surface a P&L we can't stand behind).
  pnlPercent: Nullable<number>;
}
