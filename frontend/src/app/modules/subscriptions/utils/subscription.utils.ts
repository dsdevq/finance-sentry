const DEGREES = 360;
const SATURATION = 55;
const LIGHTNESS = 42;
const HASH_SHIFT = 5;

export class SubscriptionUtils {
  public static getMerchantColor(name: string): string {
    if (!name) {
      return 'hsl(220, 14%, 50%)';
    }
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << HASH_SHIFT) - hash);
    }
    const hue = ((hash % DEGREES) + DEGREES) % DEGREES;
    return `hsl(${hue}, ${SATURATION}%, ${LIGHTNESS}%)`;
  }
}
