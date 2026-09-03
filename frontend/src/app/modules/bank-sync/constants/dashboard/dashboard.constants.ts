import {type HistoryRange} from '../../models/dashboard/dashboard.model';

/**
 * One selected range drives every dashboard widget — the net-worth chart via from/to
 * dates, the month-bucketed statistics (income vs spending, savings rate, top
 * categories) via this month count. 'all' maps to the backend's maximum window.
 */
export const HISTORY_RANGE_MONTHS: Record<HistoryRange, number> = {
  '3m': 3,
  '6m': 6,
  '1y': 12,
  all: 120,
};

/** Short display suffix for range-scoped widget labels, e.g. "Top Spending Categories (3M)". */
export const HISTORY_RANGE_LABELS: Record<HistoryRange, string> = {
  '3m': '3M',
  '6m': '6M',
  '1y': '1Y',
  all: 'All',
};

/** How far forward the net-worth projection tile looks. Fixed — there is no goal entity yet. */
export const PROJECTION_HORIZON_MONTHS = 12;

/**
 * A projection off one or two months is noise wearing a number's clothes, so the tile stays
 * hidden until the complete-month window is at least this deep.
 */
export const MIN_PROJECTION_MONTHS = 3;

/**
 * Annual market-return assumptions the projection tile offers, as fractions. 0 is the default
 * and the honest one: the app knows what the user saves, not what the market will do. The
 * non-zero options exist so a return assumption is an explicit, labelled choice rather than
 * something baked silently into the headline number.
 */
export const PROJECTION_RETURN_RATES: readonly {label: string; value: number}[] = [
  {label: '0%', value: 0},
  {label: '3%', value: 0.03},
  {label: '5%', value: 0.05},
  {label: '7%', value: 0.07},
];
