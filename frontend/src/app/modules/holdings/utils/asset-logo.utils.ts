import {environment} from '../../../../environments/environment';

const COINCAP_ICON_BASE = 'https://assets.coincap.io/assets/icons';
const LOGO_DEV_TICKER_BASE = 'https://img.logo.dev/ticker';
const LOGO_SIZE = 64;

/**
 * Builds a public logo URL for a held asset without hosting any images:
 * - crypto (Binance) → CoinCap coin icon by symbol (keyless), e.g. sol@2x.png
 * - stock/ETF (IBKR) → logo.dev by ticker, requires a publishable token
 *   (environment.logoDevToken); returns null when the token is unset.
 *
 * Returns null for anything unrecognised — callers fall back to the initials avatar.
 */
export class AssetLogoUtils {
  public static logoUrl(symbol: Nullable<string>, provider: Nullable<string>): Nullable<string> {
    const sym = symbol?.trim();
    if (!sym) {
      return null;
    }

    switch (provider?.trim().toLowerCase()) {
      case 'binance':
        return `${COINCAP_ICON_BASE}/${sym.toLowerCase()}@2x.png`;
      case 'ibkr': {
        const token = environment.logoDevToken;
        if (!token) {
          return null;
        }
        return `${LOGO_DEV_TICKER_BASE}/${sym.toUpperCase()}?token=${token}&size=${LOGO_SIZE}&format=png`;
      }
      default:
        return null;
    }
  }
}
