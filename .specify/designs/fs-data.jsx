// Finance Sentry — Mock Data
const fmt = (n, currency = 'USD') =>
  n == null ? '—' : new Intl.NumberFormat('en-US', {style:'currency',currency,minimumFractionDigits:2,maximumFractionDigits:2}).format(n);

const fmtNum = (n, d = 2) =>
  n == null ? '—' : new Intl.NumberFormat('en-US', {minimumFractionDigits:d,maximumFractionDigits:d}).format(n);

const relativeTime = ts => {
  const d = Date.now() - ts, m = Math.floor(d/60000);
  if (m < 1) return 'just now';
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m/60);
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h/24)}d ago`;
};

const fmtDate = s => {
  if (!s) return '—';
  const [y,mo,d] = s.split('-');
  return new Date(y,mo-1,d).toLocaleDateString('en-US',{month:'short',day:'numeric',year:'numeric'});
};

const INIT_ACCOUNTS = [
  {accountId:'b1', bankName:'Chase Bank', accountType:'Checking', accountNumberLast4:'4521', currentBalance:12450.00, balanceUsd:12450.00, currency:'USD', syncStatus:'synced', lastSyncTs:Date.now()-15*60e3, provider:'plaid', category:'banking'},
  {accountId:'b2', bankName:'Wells Fargo', accountType:'Savings', accountNumberLast4:'8832', currentBalance:8200.00, balanceUsd:8200.00, currency:'USD', syncStatus:'synced', lastSyncTs:Date.now()-2*60*60e3, provider:'plaid', category:'banking'},
  {accountId:'b3', bankName:'Monobank', accountType:'Current', accountNumberLast4:'1199', currentBalance:47500, balanceUsd:1150.00, currency:'UAH', syncStatus:'error', lastSyncTs:Date.now()-24*60*60e3, provider:'monobank', category:'banking'},
  {accountId:'br1', bankName:'Interactive Brokers', accountType:'Margin', accountNumberLast4:'7743', currentBalance:84320.00, balanceUsd:84320.00, currency:'USD', syncStatus:'synced', lastSyncTs:Date.now()-30*60e3, provider:'ibkr', category:'brokerage'},
  {accountId:'br2', bankName:'Interactive Brokers', accountType:'IRA', accountNumberLast4:'2210', currentBalance:42100.00, balanceUsd:42100.00, currency:'USD', syncStatus:'pending', lastSyncTs:Date.now()-5*60*60e3, provider:'ibkr', category:'brokerage'},
  {accountId:'c1', bankName:'Binance', accountType:'Spot', accountNumberLast4:null, currentBalance:15840.00, balanceUsd:15840.00, currency:'USD', syncStatus:'synced', lastSyncTs:Date.now()-5*60e3, provider:'binance', category:'crypto'},
  {accountId:'c2', bankName:'Coinbase', accountType:'Spot', accountNumberLast4:'9921', currentBalance:3200.00, balanceUsd:3200.00, currency:'USD', syncStatus:'synced', lastSyncTs:Date.now()-45*60e3, provider:'plaid', category:'crypto'},
];

const TRANSACTIONS = [
  {transactionId:'t1', postedDate:'2026-04-25', description:'Amazon.com', amount:128.50, transactionType:'debit', merchantCategory:'Shopping', isPending:false},
  {transactionId:'t2', postedDate:'2026-04-23', description:'Direct Deposit — Employer', amount:4200.00, transactionType:'credit', merchantCategory:'Income', isPending:false},
  {transactionId:'t3', postedDate:'2026-04-22', description:'Starbucks', amount:7.40, transactionType:'debit', merchantCategory:'Food & Drink', isPending:false},
  {transactionId:'t4', postedDate:'2026-04-21', description:'Netflix Subscription', amount:15.99, transactionType:'debit', merchantCategory:'Entertainment', isPending:false},
  {transactionId:'t5', pendingDate:'2026-04-20', description:'Uber Technologies', amount:23.80, transactionType:'debit', merchantCategory:'Transport', isPending:true},
  {transactionId:'t6', postedDate:'2026-04-19', description:'Whole Foods Market', amount:89.30, transactionType:'debit', merchantCategory:'Groceries', isPending:false},
  {transactionId:'t7', postedDate:'2026-04-18', description:'Con Edison Bill Pay', amount:142.00, transactionType:'debit', merchantCategory:'Utilities', isPending:false},
  {transactionId:'t8', postedDate:'2026-04-17', description:'Stripe Inc — Transfer', amount:1800.00, transactionType:'credit', merchantCategory:'Income', isPending:false},
  {transactionId:'t9', postedDate:'2026-04-16', description:'Apple App Store', amount:4.99, transactionType:'debit', merchantCategory:'Entertainment', isPending:false},
  {transactionId:'t10', postedDate:'2026-04-15', description:'Equinox Gym', amount:55.00, transactionType:'debit', merchantCategory:'Health & Fitness', isPending:false},
  {transactionId:'t11', postedDate:'2026-04-14', description:'Delta Airlines', amount:420.00, transactionType:'debit', merchantCategory:'Travel', isPending:false},
  {transactionId:'t12', postedDate:'2026-04-12', description:'Freelance Invoice #4211', amount:2450.00, transactionType:'credit', merchantCategory:'Income', isPending:false},
];

const DASHBOARD = {
  totalBalance:167260.00, accountCount:7, latestInflow:8450.00, latestOutflow:5320.00,
  netFlowData:[
    {month:'Nov', inflow:6800, outflow:5200, net:1600},
    {month:'Dec', inflow:5900, outflow:5100, net:800},
    {month:'Jan', inflow:8200, outflow:6100, net:2100},
    {month:'Feb', inflow:7400, outflow:5800, net:1600},
    {month:'Mar', inflow:9100, outflow:5970, net:3130},
    {month:'Apr', inflow:8450, outflow:5320, net:3130},
  ],
  categoryData:[
    {name:'Housing', value:1850, pct:34.7, color:'#4f46e5'},
    {name:'Food & Drink', value:1060, pct:19.9, color:'#818cf8'},
    {name:'Transport', value:795, pct:14.9, color:'#10b981'},
    {name:'Shopping', value:636, pct:11.9, color:'#f59e0b'},
    {name:'Entertainment', value:530, pct:9.9, color:'#ef4444'},
    {name:'Other', value:449, pct:8.7, color:'#9a9aaa'},
  ],
  topCategories:[
    {category:'Housing', totalSpend:1850, percentOfTotal:34.7},
    {category:'Food & Drink', totalSpend:1060, percentOfTotal:19.9},
    {category:'Transport', totalSpend:795, percentOfTotal:14.9},
    {category:'Shopping', totalSpend:636, percentOfTotal:11.9},
    {category:'Entertainment', totalSpend:530, percentOfTotal:9.9},
    {category:'Other', totalSpend:449, percentOfTotal:8.7},
  ],
};

const PROVIDERS_CONFIG = [
  {id:'plaid', name:'Plaid', description:'Connect US bank accounts & credit cards via Plaid Link', logo:'plaid',
    fields:[{id:'note', type:'info', text:'A Plaid Link popup will open to authenticate with your bank securely.'}]},
  {id:'monobank', name:'Monobank', description:'Ukrainian bank — direct API integration', logo:'monobank',
    fields:[{id:'apiToken', label:'API Token', type:'password', placeholder:'X-Token from Monobank personal cabinet'}]},
  {id:'ibkr', name:'Interactive Brokers', description:'Professional brokerage & portfolio tracking', logo:'ibkr',
    fields:[
      {id:'accountId', label:'Account ID', type:'text', placeholder:'e.g. U1234567'},
      {id:'flexToken', label:'Flex Web Service Token', type:'password', placeholder:'Your IBKR Flex token'},
    ]},
  {id:'binance', name:'Binance', description:'Crypto exchange — spot & futures balances', logo:'binance',
    fields:[
      {id:'apiKey', label:'API Key', type:'text', placeholder:'Binance API key (read-only)'},
      {id:'secretKey', label:'Secret Key', type:'password', placeholder:'Binance secret key'},
    ]},
];

// 13-month net worth history, broken down by category
const NET_WORTH_HISTORY = [
  {month:'Apr 25', banking:18400, brokerage:95200, crypto:9800,  total:123400},
  {month:'May 25', banking:19100, brokerage:97600, crypto:11200, total:127900},
  {month:'Jun 25', banking:17800, brokerage:99400, crypto:8400,  total:125600},
  {month:'Jul 25', banking:20300, brokerage:103200,crypto:12600, total:136100},
  {month:'Aug 25', banking:21200, brokerage:106800,crypto:14100, total:142100},
  {month:'Sep 25', banking:19600, brokerage:104100,crypto:10200, total:133900},
  {month:'Oct 25', banking:22100, brokerage:108400,crypto:15800, total:146300},
  {month:'Nov 25', banking:21800, brokerage:111600,crypto:17200, total:150600},
  {month:'Dec 25', banking:23400, brokerage:115200,crypto:13900, total:152500},
  {month:'Jan 26', banking:22700, brokerage:118400,crypto:16400, total:157500},
  {month:'Feb 26', banking:21900, brokerage:122100,crypto:14700, total:158700},
  {month:'Mar 26', banking:22600, brokerage:124800,crypto:17300, total:164700},
  {month:'Apr 26', banking:21800, brokerage:126420,crypto:19040, total:167260},
];

const BUDGETS = [
  {category:'Housing',        limit:2000, spent:1850, color:'#4f46e5'},
  {category:'Food & Drink',   limit:1200, spent:1060, color:'#818cf8'},
  {category:'Transport',      limit:600,  spent:795,  color:'#10b981'},
  {category:'Shopping',       limit:800,  spent:636,  color:'#f59e0b'},
  {category:'Entertainment',  limit:400,  spent:530,  color:'#ef4444'},
  {category:'Health & Fitness',limit:150, spent:55,   color:'#06b6d4'},
  {category:'Utilities',      limit:200,  spent:142,  color:'#8b5cf6'},
  {category:'Travel',         limit:500,  spent:420,  color:'#f97316'},
];

const SUBSCRIPTIONS = [
  {id:'s1', name:'Netflix',      category:'Entertainment', amount:15.99,  frequency:'monthly', nextDate:'2026-05-21', status:'active',  logo:'N', color:'#e50914'},
  {id:'s2', name:'Equinox Gym',  category:'Health',        amount:55.00,  frequency:'monthly', nextDate:'2026-05-15', status:'active',  logo:'E', color:'#1a1a1a'},
  {id:'s3', name:'Apple iCloud', category:'Storage',       amount:2.99,   frequency:'monthly', nextDate:'2026-05-08', status:'active',  logo:'A', color:'#555'},
  {id:'s4', name:'Con Edison',   category:'Utilities',     amount:142.00, frequency:'monthly', nextDate:'2026-05-18', status:'active',  logo:'C', color:'#003087'},
  {id:'s5', name:'Spotify',      category:'Entertainment', amount:9.99,   frequency:'monthly', nextDate:'2026-05-03', status:'active',  logo:'S', color:'#1db954'},
  {id:'s6', name:'Adobe CC',     category:'Software',      amount:54.99,  frequency:'monthly', nextDate:'2026-05-12', status:'active',  logo:'A', color:'#ff0000'},
  {id:'s7', name:'Amazon Prime', category:'Shopping',      amount:14.99,  frequency:'monthly', nextDate:'2026-05-22', status:'active',  logo:'a', color:'#ff9900'},
  {id:'s8', name:'NY Times',     category:'Media',         amount:4.00,   frequency:'monthly', nextDate:'2026-06-01', status:'paused',  logo:'T', color:'#333'},
];

const USER_PROFILE = {
  firstName: 'John',
  lastName: 'Doe',
  email: 'john.doe@example.com',
  baseCurrency: 'USD',
  twoFactor: false,
  emailAlerts: true,
  lowBalanceThreshold: 500,
  theme: 'system',
};

const ALERTS = [
  {id:'a1', type:'sync_error',   severity:'error',   title:'Monobank sync failed',              body:'Unable to connect to Monobank API. Last successful sync was 24 hours ago. Check your API token in Settings.',  account:'Monobank', ts: Date.now()-2*60*60e3,  read:false},
  {id:'a2', type:'low_balance',  severity:'warning', title:'Low balance — Chase Checking',      body:'Your Chase Checking account (····4521) dropped below your $500 threshold. Current balance: $312.40.',           account:'Chase Bank', ts: Date.now()-5*60*60e3, read:false},
  {id:'a3', type:'unusual_spend',severity:'warning', title:'Unusual spending detected',         body:'$420 charge from Delta Airlines is 3× larger than your average travel transaction. Flagged for review.',         account:'Chase Bank', ts: Date.now()-18*60*60e3,read:false},
  {id:'a4', type:'sync_error',   severity:'error',   title:'IBKR IRA sync pending >4h',         body:'Interactive Brokers IRA (····2210) has been in "pending" state for over 4 hours. This may indicate a Flex token issue.', account:'IBKR', ts: Date.now()-6*60*60e3, read:true},
  {id:'a5', type:'budget',       severity:'warning', title:'Transport budget exceeded',         body:'You\'ve spent $795 against a $600 transport budget this month — 32% over. Consider reviewing your ride-share usage.', account:null, ts: Date.now()-24*60*60e3, read:true},
  {id:'a6', type:'budget',       severity:'warning', title:'Entertainment budget at 133%',      body:'Netflix, Apple App Store, and other entertainment charges total $530 against a $400 limit.',                    account:null, ts: Date.now()-30*60*60e3, read:true},
  {id:'a7', type:'info',         severity:'info',    title:'Binance sync completed',            body:'Binance Spot account synced successfully. Portfolio value updated to $15,840.',                                   account:'Binance', ts: Date.now()-45*60e3,   read:true},
  {id:'a8', type:'info',         severity:'info',    title:'Weekly net worth report ready',     body:'Your net worth increased by $2,560 (+1.6%) this week. View the full breakdown in Holdings.',                     account:null, ts: Date.now()-3*24*60*60e3, read:true},
];

const PORTFOLIO_HOLDINGS = [
  // IBKR positions
  {id:'p1', accountId:'br1', symbol:'AAPL',  name:'Apple Inc.',          qty:15,   avgCost:168.40, currentPrice:189.30, currency:'USD'},
  {id:'p2', accountId:'br1', symbol:'MSFT',  name:'Microsoft Corp.',     qty:10,   avgCost:385.20, currentPrice:420.15, currency:'USD'},
  {id:'p3', accountId:'br1', symbol:'NVDA',  name:'NVIDIA Corp.',        qty:8,    avgCost:520.00, currentPrice:875.40, currency:'USD'},
  {id:'p4', accountId:'br1', symbol:'SPY',   name:'S&P 500 ETF',         qty:20,   avgCost:480.00, currentPrice:524.80, currency:'USD'},
  {id:'p5', accountId:'br1', symbol:'BND',   name:'Vanguard Bond ETF',   qty:50,   avgCost:71.20,  currentPrice:69.40,  currency:'USD'},
  {id:'p6', accountId:'br2', symbol:'VTI',   name:'Vanguard Total Mkt',  qty:60,   avgCost:215.00, currentPrice:248.60, currency:'USD'},
  {id:'p7', accountId:'br2', symbol:'QQQM',  name:'Nasdaq 100 ETF',      qty:12,   avgCost:162.00, currentPrice:198.30, currency:'USD'},
  // Binance positions
  {id:'p8', accountId:'c1', symbol:'BTC',   name:'Bitcoin',              qty:0.18, avgCost:42000,  currentPrice:64800,  currency:'USD'},
  {id:'p9', accountId:'c1', symbol:'ETH',   name:'Ethereum',             qty:2.4,  avgCost:2200,   currentPrice:3150,   currency:'USD'},
  {id:'p10',accountId:'c1', symbol:'SOL',   name:'Solana',               qty:22,   avgCost:95,     currentPrice:148,    currency:'USD'},
];

Object.assign(window, {INIT_ACCOUNTS, TRANSACTIONS, DASHBOARD, PROVIDERS_CONFIG,
  NET_WORTH_HISTORY, USER_PROFILE, BUDGETS, SUBSCRIPTIONS,
  ALERTS, PORTFOLIO_HOLDINGS, fmt, fmtNum, relativeTime, fmtDate});
