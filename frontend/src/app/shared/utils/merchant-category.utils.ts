export class MerchantCategoryUtils {
  public static format(category: string): string {
    return category
      .replace(/_AND_/g, ' & ')
      .split('_')
      .map(w => w.charAt(0).toUpperCase() + w.slice(1).toLowerCase())
      .join(' ');
  }
}
