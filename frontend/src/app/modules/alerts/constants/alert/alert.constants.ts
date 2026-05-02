import {type Alert} from '../../models/alert/alert.model';

const MS_PER_SECOND = 1000;
const SECONDS_PER_MINUTE = 60;
const MINUTES_PER_HOUR = 60;
const HOURS_PER_DAY = 24;
const MS_PER_MINUTE = SECONDS_PER_MINUTE * MS_PER_SECOND;
const MS_PER_HOUR = MINUTES_PER_HOUR * MS_PER_MINUTE;
const MS_PER_DAY = HOURS_PER_DAY * MS_PER_HOUR;

const OFFSET_2H = 2;
const OFFSET_5H = 5;
const OFFSET_6H = 6;
const OFFSET_18H = 18;
const OFFSET_30M = 30;
const OFFSET_45M = 45;
const OFFSET_3D = 3;

const now = Date.now();

const H2 = OFFSET_2H * MS_PER_HOUR;
const H5 = OFFSET_5H * MS_PER_HOUR;
const H6 = OFFSET_6H * MS_PER_HOUR;
const H18 = OFFSET_18H * MS_PER_HOUR;
const M30 = OFFSET_30M * MS_PER_MINUTE;
const M45 = OFFSET_45M * MS_PER_MINUTE;
const D3 = OFFSET_3D * MS_PER_DAY;

export const ALERT_MOCK_DATA: Alert[] = [
  {
    id: 'a1',
    type: 'sync_error',
    severity: 'error',
    title: 'Monobank sync failed',
    body: 'Unable to connect to Monobank API. Last successful sync was 24 hours ago. Check your API token in Settings.',
    account: 'Monobank',
    timestamp: now - H2,
    read: false,
  },
  {
    id: 'a2',
    type: 'low_balance',
    severity: 'warning',
    title: 'Low balance — Chase Checking',
    body: 'Your Chase Checking account (····4521) dropped below your $500 threshold. Current balance: $312.40.',
    account: 'Chase Bank',
    timestamp: now - H5,
    read: false,
  },
  {
    id: 'a3',
    type: 'unusual_spend',
    severity: 'warning',
    title: 'Unusual spending detected',
    body: '$420 charge from Delta Airlines is 3× larger than your average travel transaction. Flagged for review.',
    account: 'Chase Bank',
    timestamp: now - H18,
    read: false,
  },
  {
    id: 'a4',
    type: 'sync_error',
    severity: 'error',
    title: 'IBKR IRA sync pending >4h',
    body: 'Interactive Brokers IRA (····2210) has been in "pending" state for over 4 hours. This may indicate a Flex token issue.',
    account: 'IBKR',
    timestamp: now - H6,
    read: true,
  },
  {
    id: 'a5',
    type: 'budget',
    severity: 'warning',
    title: 'Transport budget exceeded',
    body: "You've spent $795 against a $600 transport budget this month — 32% over. Consider reviewing your ride-share usage.",
    account: null,
    timestamp: now - MS_PER_DAY,
    read: true,
  },
  {
    id: 'a6',
    type: 'budget',
    severity: 'warning',
    title: 'Entertainment budget at 133%',
    body: 'Netflix, Apple App Store, and other entertainment charges total $530 against a $400 limit.',
    account: null,
    timestamp: now - M30,
    read: true,
  },
  {
    id: 'a7',
    type: 'info',
    severity: 'info',
    title: 'Binance sync completed',
    body: 'Binance Spot account synced successfully. Portfolio value updated to $15,840.',
    account: 'Binance',
    timestamp: now - M45,
    read: true,
  },
  {
    id: 'a8',
    type: 'info',
    severity: 'info',
    title: 'Weekly net worth report ready',
    body: 'Your net worth increased by $2,560 (+1.6%) this week. View the full breakdown in Holdings.',
    account: null,
    timestamp: now - D3,
    read: true,
  },
];
