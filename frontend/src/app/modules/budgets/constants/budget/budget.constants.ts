const MONTHS_IN_YEAR = 12;

export const BUDGETS_MONTHS_IN_YEAR = MONTHS_IN_YEAR;

export const CATEGORY_COLOR_MAP = new Map<string, string>([
  ['housing', '#4f46e5'],
  ['food_and_drink', '#818cf8'],
  ['transport', '#10b981'],
  ['shopping', '#f59e0b'],
  ['entertainment', '#ef4444'],
  ['health', '#06b6d4'],
  ['utilities', '#8b5cf6'],
  ['travel', '#f97316'],
  ['other', '#94a3b8'],
]);

export const CATEGORY_COLOR_FALLBACK = '#94a3b8';

export const VALID_BUDGET_CATEGORIES: {key: string; label: string}[] = [
  {key: 'housing', label: 'Housing'},
  {key: 'food_and_drink', label: 'Food & Drink'},
  {key: 'transport', label: 'Transport'},
  {key: 'shopping', label: 'Shopping'},
  {key: 'entertainment', label: 'Entertainment'},
  {key: 'health', label: 'Health & Fitness'},
  {key: 'utilities', label: 'Utilities'},
  {key: 'travel', label: 'Travel'},
  {key: 'other', label: 'Other'},
];
