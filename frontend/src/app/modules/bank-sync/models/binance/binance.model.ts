export interface ConnectBinanceRequest {
  apiKey: string;
  secretKey: string;
}

export interface ConnectBinanceResponse {
  accountId: string;
  message: string;
}
