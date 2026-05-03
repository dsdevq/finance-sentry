export interface DateRange {
  from: string | null;
  to: string;
}

export type RelativeRange = '3m' | '6m' | '1y' | 'all';

const ISO_DATE_LENGTH = 10;
const MONTHS_3 = 3;
const MONTHS_6 = 6;
const PAD_WIDTH = 2;

export class DateRangeUtils {
  public static toIsoDate(d: Date): string {
    return d.toISOString().slice(0, ISO_DATE_LENGTH);
  }

  public static endOfMonthUtc(d: Date): string {
    const daysInMonth = new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth() + 1, 0)).getUTCDate();
    return `${d.getUTCFullYear()}-${String(d.getUTCMonth() + 1).padStart(PAD_WIDTH, '0')}-${String(daysInMonth).padStart(PAD_WIDTH, '0')}`;
  }

  public static fromRelativeRange(range: RelativeRange, now = new Date()): DateRange {
    const to = DateRangeUtils.endOfMonthUtc(now);
    const d = new Date(now);

    if (range === '3m') {
      d.setMonth(d.getMonth() - MONTHS_3);
    } else if (range === '6m') {
      d.setMonth(d.getMonth() - MONTHS_6);
    } else if (range === '1y') {
      d.setFullYear(d.getFullYear() - 1);
    } else {
      return {from: null, to};
    }

    return {from: DateRangeUtils.toIsoDate(d), to};
  }

  public static toHttpParams(range: DateRange): Record<string, string> {
    return range.from ? {from: range.from, to: range.to} : {to: range.to};
  }
}
