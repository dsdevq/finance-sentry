import {
  BANK_NAME_DOMAINS,
  PROVIDER_DOMAINS,
} from '../constants/institution/institution-domains.constants';

// DuckDuckGo's icon service returns the image directly (HTTP 200), unlike
// Google's s2/favicons which 301-redirects — some image pipelines don't follow
// the redirect, so provider logos silently failed to render.
const FAVICON_ENDPOINT = 'https://icons.duckduckgo.com/ip3';

/**
 * Resolves a connected institution to a public favicon URL (DuckDuckGo's icon
 * service) so we render real provider logos without hosting any images.
 * Returns null when the institution isn't recognised — callers fall back to the
 * initials avatar.
 */
export class InstitutionLogoUtils {
  public static faviconUrl(provider: Nullable<string>, name: Nullable<string>): Nullable<string> {
    const domain = InstitutionLogoUtils.resolveDomain(provider, name);
    if (!domain) {
      return null;
    }
    return `${FAVICON_ENDPOINT}/${domain}.ico`;
  }

  private static resolveDomain(
    provider: Nullable<string>,
    name: Nullable<string>
  ): Nullable<string> {
    const providerKey = provider?.trim().toLowerCase() ?? '';
    if (providerKey && PROVIDER_DOMAINS[providerKey]) {
      return PROVIDER_DOMAINS[providerKey];
    }

    const nameKey = name?.trim().toLowerCase() ?? '';
    if (!nameKey) {
      return null;
    }
    for (const [keyword, domain] of BANK_NAME_DOMAINS) {
      if (nameKey.includes(keyword)) {
        return domain;
      }
    }
    return null;
  }
}
