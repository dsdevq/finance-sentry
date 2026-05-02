// Finance Sentry — Main Pages (Dashboard, Accounts, Transactions, Holdings)
const { useState, useEffect, useMemo } = React;

const P = { padding:'24px', maxWidth:1200, margin:'0 auto' };

// ── Dashboard ─────────────────────────────────────────────────────────────────
function DashboardPage({ accounts }) {
  const [loading, setLoading] = useState(true);
  useEffect(() => { const t=setTimeout(()=>setLoading(false),900); return ()=>clearTimeout(t); },[]);
  const d = DASHBOARD;
  const totalBanking   = accounts.filter(a=>a.category==='banking').reduce((s,a)=>s+a.balanceUsd,0);
  const totalBrokerage = accounts.filter(a=>a.category==='brokerage').reduce((s,a)=>s+a.balanceUsd,0);
  const totalCrypto    = accounts.filter(a=>a.category==='crypto').reduce((s,a)=>s+a.balanceUsd,0);
  const totalBalance   = totalBanking+totalBrokerage+totalCrypto;

  return (
    <div style={{ padding:24 }}>
      <div style={{ maxWidth:1200, margin:'0 auto', display:'flex', flexDirection:'column', gap:20 }}>
        {/* KPI row */}
        <div style={{ display:'grid', gridTemplateColumns:'repeat(4,1fr)', gap:16 }}>
          <StatCard label="Total Balance" value={loading?'—':fmt(totalBalance)} icon="Wallet" loading={loading} delta={3.2} deltaLabel="vs last month" />
          <StatCard label="Accounts" value={loading?'—':String(accounts.length)} icon="Building2" loading={loading} />
          <StatCard label="Monthly Inflow" value={loading?'—':fmt(d.latestInflow)} icon="TrendingUp" loading={loading} delta={12.1} deltaLabel="vs Nov" />
          <StatCard label="Monthly Outflow" value={loading?'—':fmt(d.latestOutflow)} icon="TrendingDown" loading={loading} delta={-4.3} deltaLabel="vs Nov" />
        </div>

        {/* Net worth history — full width */}
        {loading
          ? <Skeleton height={260} radius={8} />
          : <NetWorthChart data={NET_WORTH_HISTORY} />}

        {/* Charts row */}
        <div style={{ display:'grid', gridTemplateColumns:'1fr 380px', gap:16 }}>
          {loading
            ? <><Skeleton height={220} radius={8} /><Skeleton height={220} radius={8} /></>
            : <><LineChart data={d.netFlowData} label="Monthly Net Cash Flow" currency="USD" />
               <DonutChart segments={d.categoryData} label="Top Spending Categories" /></>}
        </div>

        {/* Category table */}
        <DataTable loading={loading}
          columns={[
            { key:'category', header:'Category', cell:r=>r.category },
            { key:'spend', header:'Total Spend', align:'right', mono:true,
              cell:r=><span style={{fontFamily:'JetBrains Mono,monospace'}}>{fmt(r.totalSpend)}</span> },
            { key:'pct', header:'% of Total', align:'right',
              cell:r=><div style={{display:'flex',alignItems:'center',gap:10,justifyContent:'flex-end'}}>
                <div style={{width:80,height:5,borderRadius:3,background:'var(--surface-raised)',overflow:'hidden'}}>
                  <div style={{height:'100%',borderRadius:3,background:'var(--accent-default)',width:`${r.percentOfTotal}%`}} />
                </div>
                <span style={{fontFamily:'JetBrains Mono,monospace',fontSize:'12px'}}>{r.percentOfTotal.toFixed(1)}%</span>
              </div> },
          ]}
          rows={d.topCategories} emptyMessage="No spending data available" />
      </div>
    </div>
  );
}

// ── Accounts List ─────────────────────────────────────────────────────────────
function AccountsListPage({ accounts, onConnect, onDisconnect, onViewTransactions }) {
  const [loading, setLoading] = useState(true);
  useEffect(() => { const t=setTimeout(()=>setLoading(false),700); return ()=>clearTimeout(t); },[]);

  const banking   = accounts.filter(a=>a.category==='banking');
  const brokerage = accounts.filter(a=>a.category==='brokerage');
  const crypto    = accounts.filter(a=>a.category==='crypto');
  const totalNetWorth = accounts.reduce((s,a)=>s+a.balanceUsd,0);

  const AccountTable = ({ rows, title, showDisconnect=false }) => (
    <div style={{ marginBottom:28 }}>
      <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between', marginBottom:10 }}>
        <span style={{ fontSize:'11px', fontWeight:600, letterSpacing:'0.07em', textTransform:'uppercase',
          color:'var(--text-secondary)' }}>{title}</span>
        <span style={{ fontSize:'11px', color:'var(--text-disabled)' }}>
          {rows.length} account{rows.length!==1?'s':''}
        </span>
      </div>
      <Card padding="none">
        <table style={{ width:'100%', borderCollapse:'collapse', fontSize:'13px' }}>
          <thead>
            <tr style={{ borderBottom:'1px solid var(--border-default)' }}>
              {['Institution','Type','Connectivity','Balance',showDisconnect?'':''].map((h,i) =>
                <th key={i} style={{ padding:'9px 16px', fontSize:'11px', fontWeight:600,
                  letterSpacing:'0.06em', textTransform:'uppercase', color:'var(--text-secondary)',
                  textAlign: i>=3 ? 'right' : 'left', whiteSpace:'nowrap' }}>{h}</th>
              )}
            </tr>
          </thead>
          <tbody>
            {rows.map((acc, ri) => (
              <tr key={acc.accountId}
                style={{ borderBottom: ri<rows.length-1?'1px solid var(--border-default)':undefined, cursor:'pointer', transition:'background 100ms' }}
                onMouseEnter={e=>e.currentTarget.style.background='var(--surface-bg)'}
                onMouseLeave={e=>e.currentTarget.style.background=''}
                onClick={() => onViewTransactions(acc)}>
                <td style={{ padding:'14px 16px' }}>
                  <div style={{ display:'flex', alignItems:'center', gap:12 }}>
                    <InstitutionAvatar name={acc.bankName} />
                    <div>
                      <div style={{ fontWeight:500, color:'var(--text-primary)', marginBottom:2 }}>{acc.bankName}</div>
                      <div style={{ fontSize:'11px', color:'var(--text-secondary)' }}>
                        {acc.accountNumberLast4 ? `•••• ${acc.accountNumberLast4}` : acc.provider}
                      </div>
                    </div>
                  </div>
                </td>
                <td style={{ padding:'14px 16px', color:'var(--text-secondary)' }}>{acc.accountType}</td>
                <td style={{ padding:'14px 16px' }}>
                  <StatusIndicator status={acc.syncStatus} timestamp={relativeTime(acc.lastSyncTs)} />
                </td>
                <td style={{ padding:'14px 16px', textAlign:'right', fontFamily:'JetBrains Mono,monospace',
                  fontWeight:500, color:'var(--text-primary)' }}>
                  <div>{acc.currency!=='USD' ? `${fmtNum(acc.currentBalance)} ${acc.currency}` : fmt(acc.currentBalance)}</div>
                  {acc.currency!=='USD' && <div style={{ fontSize:'11px', color:'var(--text-disabled)' }}>≈ {fmt(acc.balanceUsd)}</div>}
                </td>
                {showDisconnect && (
                  <td style={{ padding:'14px 16px', textAlign:'right' }} onClick={e=>e.stopPropagation()}>
                    <Button variant="secondary" size="sm" onClick={()=>onDisconnect(acc)}>Disconnect</Button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );

  return (
    <div style={{ padding:24 }}>
      <div style={{ maxWidth:1200, margin:'0 auto' }}>
        {/* Page header */}
        <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between', marginBottom:28 }}>
          <div>
            <h1 style={{ fontSize:'22px', fontWeight:700, color:'var(--text-primary)' }}>Account Inventory</h1>
            <p style={{ fontSize:'13px', color:'var(--text-secondary)', marginTop:4 }}>All connected financial accounts</p>
          </div>
          <div style={{ display:'flex', alignItems:'center', gap:24 }}>
            {!loading && totalNetWorth > 0 && (
              <div style={{ textAlign:'right' }}>
                <div style={{ fontSize:'11px', textTransform:'uppercase', letterSpacing:'0.06em', color:'var(--text-secondary)', marginBottom:2 }}>Total Net Worth</div>
                <div style={{ fontFamily:'JetBrains Mono,monospace', fontSize:'22px', fontWeight:700, color:'var(--text-primary)' }}>{fmt(totalNetWorth)}</div>
              </div>
            )}
            <Button icon="Plus" onClick={onConnect}>Connect Account</Button>
          </div>
        </div>

        {loading ? (
          <div style={{ display:'flex', flexDirection:'column', gap:16 }}>
            <Skeleton height={200} radius={8} />
            <Skeleton height={160} radius={8} />
          </div>
        ) : accounts.length === 0 ? (
          <Card style={{ padding:48, textAlign:'center' }}>
            <div style={{ fontSize:'13px', color:'var(--text-secondary)', marginBottom:16 }}>No accounts connected yet.</div>
            <Button onClick={onConnect}>Connect Your First Account</Button>
          </Card>
        ) : (
          <>
            {banking.length>0 && <AccountTable rows={banking} title="Banking Institutions" showDisconnect />}
            {brokerage.length>0 && <AccountTable rows={brokerage} title="Brokerage & Investment" />}
            {crypto.length>0 && <AccountTable rows={crypto} title="Digital Assets" showDisconnect />}
            <div style={{ display:'flex', justifyContent:'space-between', fontSize:'12px',
              color:'var(--text-disabled)', borderTop:'1px solid var(--border-default)', paddingTop:14 }}>
              <span>{accounts.length} total connection{accounts.length!==1?'s':''}</span>
              <span>Net worth: {fmt(totalNetWorth)}</span>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

// ── Transaction Detail Drawer ─────────────────────────────────────────────────
const CATEGORIES = ['Shopping','Food & Drink','Transport','Entertainment','Health & Fitness',
  'Utilities','Travel','Groceries','Income','Other'];

function TxDrawer({ tx, onClose }) {
  const { addToast } = useToast();
  const [category, setCategory] = useState(tx?.merchantCategory || 'Other');
  const [note, setNote] = useState('');

  useEffect(() => {
    if (tx) { setCategory(tx.merchantCategory || 'Other'); setNote(''); }
  }, [tx?.transactionId]);

  if (!tx) return null;

  const isCredit = tx.transactionType === 'credit';

  return (
    <>
      {/* Backdrop */}
      <div onClick={onClose} style={{ position:'fixed', inset:0, zIndex:400,
        background:'rgba(0,0,0,.25)', animation:'fadeIn .15s ease' }} />

      {/* Panel */}
      <div style={{ position:'fixed', top:0, right:0, bottom:0, width:380, zIndex:401,
        background:'var(--surface-card)', borderLeft:'1px solid var(--border-default)',
        boxShadow:'var(--shadow-md)', display:'flex', flexDirection:'column',
        animation:'slideInRight .2s ease' }}>

        {/* Header */}
        <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between',
          padding:'18px 20px', borderBottom:'1px solid var(--border-default)', flexShrink:0 }}>
          <div style={{ fontSize:'15px', fontWeight:600, color:'var(--text-primary)' }}>Transaction Detail</div>
          <button onClick={onClose} style={{ background:'none', border:'none', cursor:'pointer',
            padding:5, borderRadius:6, color:'var(--text-secondary)', display:'flex',
            transition:'background 120ms' }}
            onMouseEnter={e=>e.currentTarget.style.background='var(--surface-raised)'}
            onMouseLeave={e=>e.currentTarget.style.background='none'}>
            <Icon name="X" size="sm" />
          </button>
        </div>

        {/* Body */}
        <div style={{ flex:1, overflowY:'auto', padding:'20px' }}>
          {/* Amount hero */}
          <div style={{ textAlign:'center', padding:'24px 0 20px', borderBottom:'1px solid var(--border-default)', marginBottom:20 }}>
            <div style={{ fontFamily:'JetBrains Mono,monospace', fontSize:'32px', fontWeight:700,
              color: isCredit ? 'var(--status-success)' : 'var(--text-primary)',
              marginBottom:6 }}>
              {isCredit ? '+' : '-'}{fmt(tx.amount)}
            </div>
            <Badge variant={tx.isPending ? 'warning' : 'neutral'}>
              {tx.isPending ? 'Pending' : 'Posted'}
            </Badge>
          </div>

          {/* Details grid */}
          {[
            ['Description', tx.description],
            ['Date',        fmtDate(tx.postedDate || tx.pendingDate)],
            ['Type',        tx.transactionType.charAt(0).toUpperCase() + tx.transactionType.slice(1)],
          ].map(([label, value]) => (
            <div key={label} style={{ display:'flex', justifyContent:'space-between',
              padding:'10px 0', borderBottom:'1px solid var(--border-default)' }}>
              <span style={{ fontSize:'12px', color:'var(--text-secondary)' }}>{label}</span>
              <span style={{ fontSize:'13px', fontWeight:500, color:'var(--text-primary)',
                maxWidth:220, textAlign:'right' }}>{value}</span>
            </div>
          ))}

          {/* Category picker */}
          <div style={{ padding:'16px 0 8px' }}>
            <div style={{ fontSize:'11px', fontWeight:600, letterSpacing:'0.07em',
              textTransform:'uppercase', color:'var(--text-secondary)', marginBottom:10 }}>Category</div>
            <div style={{ display:'flex', flexWrap:'wrap', gap:6 }}>
              {CATEGORIES.map(cat => (
                <button key={cat} onClick={() => setCategory(cat)}
                  style={{ padding:'5px 11px', borderRadius:20, fontSize:'12px', fontWeight:500,
                    border:'1.5px solid', cursor:'pointer', fontFamily:'inherit', transition:'all 120ms',
                    borderColor: category===cat ? 'var(--accent-default)' : 'var(--border-default)',
                    background:  category===cat ? 'var(--accent-subtle)'  : 'transparent',
                    color:       category===cat ? 'var(--accent-default)' : 'var(--text-secondary)' }}>
                  {cat}
                </button>
              ))}
            </div>
          </div>

          {/* Note */}
          <div style={{ marginTop:16 }}>
            <div style={{ fontSize:'11px', fontWeight:600, letterSpacing:'0.07em',
              textTransform:'uppercase', color:'var(--text-secondary)', marginBottom:8 }}>Note</div>
            <textarea value={note} onChange={e=>setNote(e.target.value)}
              placeholder="Add a personal note…" rows={3}
              style={{ width:'100%', padding:'9px 12px', borderRadius:6,
                border:'1.5px solid var(--border-default)', background:'var(--surface-bg)',
                color:'var(--text-primary)', fontSize:'13px', outline:'none', resize:'vertical',
                fontFamily:'inherit', lineHeight:1.5, boxSizing:'border-box' }} />
          </div>
        </div>

        {/* Footer */}
        <div style={{ padding:'14px 20px', borderTop:'1px solid var(--border-default)',
          display:'flex', gap:8, flexShrink:0 }}>
          <Button variant="secondary" onClick={onClose} fullWidth>Cancel</Button>
          <Button fullWidth onClick={() => { addToast('Transaction updated', 'success'); onClose(); }}>Save</Button>
        </div>
      </div>
    </>
  );
}

// ── Transaction List ──────────────────────────────────────────────────────────
const PAGE_SIZE = 7;

function TransactionListPage({ account, onBack }) {
  const [loading, setLoading] = useState(true);
  const [startDate, setStartDate] = useState('2026-04-01');
  const [endDate, setEndDate] = useState('2026-04-27');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [selectedTx, setSelectedTx] = useState(null);

  useEffect(() => { const t=setTimeout(()=>setLoading(false),800); return ()=>clearTimeout(t); },[]);

  const filtered = useMemo(() => TRANSACTIONS.filter(tx => {
    const d = tx.postedDate || tx.pendingDate || '';
    const inRange = (!startDate||d>=startDate) && (!endDate||d<=endDate);
    const matchSearch = !search || tx.description.toLowerCase().includes(search.toLowerCase()) ||
      (tx.merchantCategory||'').toLowerCase().includes(search.toLowerCase());
    return inRange && matchSearch;
  }), [startDate, endDate, search]);

  const totalPages = Math.ceil(filtered.length / PAGE_SIZE);
  const pageTxs = filtered.slice((page-1)*PAGE_SIZE, page*PAGE_SIZE);

  const TypeBadge = ({ tx }) => (
    <Badge variant={tx.transactionType==='credit'?'success':'neutral'}>
      {tx.transactionType}
    </Badge>
  );
  const StatusBadge = ({ tx }) => (
    <Badge variant={tx.isPending?'warning':'neutral'}>
      {tx.isPending?'Pending':'Posted'}
    </Badge>
  );

  return (
    <div style={{ padding:24 }}>
      <div style={{ maxWidth:1200, margin:'0 auto' }}>
        {/* Header */}
        <div style={{ display:'flex', alignItems:'center', gap:14, marginBottom:24 }}>
          <button onClick={onBack}
            style={{ display:'flex', alignItems:'center', gap:6, padding:'7px 12px',
              background:'var(--surface-card)', border:'1px solid var(--border-default)',
              borderRadius:8, cursor:'pointer', fontSize:'13px', color:'var(--text-secondary)',
              fontFamily:'inherit', transition:'all 120ms' }}
            onMouseEnter={e=>{e.currentTarget.style.background='var(--surface-raised)';e.currentTarget.style.color='var(--text-primary)'}}
            onMouseLeave={e=>{e.currentTarget.style.background='var(--surface-card)';e.currentTarget.style.color='var(--text-secondary)'}}>
            <Icon name="ChevronLeft" size="xs" />Back
          </button>
          <div>
            <h1 style={{ fontSize:'20px', fontWeight:700, color:'var(--text-primary)' }}>
              {account ? account.bankName : 'All'} Transactions
            </h1>
            {account && <p style={{ fontSize:'12px', color:'var(--text-secondary)', marginTop:2 }}>
              {account.accountType} •••• {account.accountNumberLast4||account.provider} · {account.currency}
            </p>}
          </div>
        </div>

        {/* Filters */}
        <Card style={{ marginBottom:16, padding:'14px 16px' }}>
          <div style={{ display:'flex', alignItems:'center', gap:12, flexWrap:'wrap' }}>
            <Icon name="Filter" size="xs" style={{ color:'var(--text-secondary)' }} />
            <div style={{ display:'flex', alignItems:'center', gap:8 }}>
              <span style={{ fontSize:'12px', color:'var(--text-secondary)' }}>From</span>
              <input type="date" value={startDate} onChange={e=>{setStartDate(e.target.value);setPage(1)}}
                style={{ padding:'6px 10px', borderRadius:6, border:'1px solid var(--border-default)',
                  background:'var(--surface-bg)', color:'var(--text-primary)', fontSize:'13px', outline:'none' }} />
            </div>
            <div style={{ display:'flex', alignItems:'center', gap:8 }}>
              <span style={{ fontSize:'12px', color:'var(--text-secondary)' }}>To</span>
              <input type="date" value={endDate} onChange={e=>{setEndDate(e.target.value);setPage(1)}}
                style={{ padding:'6px 10px', borderRadius:6, border:'1px solid var(--border-default)',
                  background:'var(--surface-bg)', color:'var(--text-primary)', fontSize:'13px', outline:'none' }} />
            </div>
            <div style={{ flex:1, minWidth:180 }}>
              <input type="text" value={search} onChange={e=>{setSearch(e.target.value);setPage(1)}}
                placeholder="Search transactions…"
                style={{ width:'100%', padding:'7px 12px', borderRadius:6,
                  border:'1px solid var(--border-default)', background:'var(--surface-bg)',
                  color:'var(--text-primary)', fontSize:'13px', outline:'none' }} />
            </div>
            {(search||startDate||endDate) && (
              <Button variant="ghost" size="sm" onClick={()=>{setSearch('');setStartDate('');setEndDate('');setPage(1)}}>
                Clear
              </Button>
            )}
          </div>
        </Card>

        {/* Summary strip */}
        {!loading && filtered.length > 0 && (() => {
          const credits = filtered.filter(t=>t.transactionType==='credit').reduce((s,t)=>s+t.amount,0);
          const debits  = filtered.filter(t=>t.transactionType==='debit').reduce((s,t)=>s+t.amount,0);
          return (
            <div style={{ display:'grid', gridTemplateColumns:'repeat(3,1fr)', gap:12, marginBottom:16 }}>
              {[['Total Inflow', fmt(credits), 'var(--status-success)'],
                ['Total Outflow', fmt(debits), 'var(--status-error)'],
                ['Net', fmt(credits-debits), credits-debits>=0?'var(--status-success)':'var(--status-error)']
               ].map(([l,v,c])=>(
                <div key={l} style={{ background:'var(--surface-card)', border:'1px solid var(--border-default)',
                  borderRadius:8, padding:'12px 16px' }}>
                  <div style={{ fontSize:'11px', textTransform:'uppercase', letterSpacing:'0.06em',
                    color:'var(--text-secondary)', marginBottom:4 }}>{l}</div>
                  <div style={{ fontFamily:'JetBrains Mono,monospace', fontSize:'16px', fontWeight:600, color:c }}>{v}</div>
                </div>
              ))}
            </div>
          );
        })()}

        {/* Table */}
        {loading ? <Skeleton height={320} radius={8} /> : (
          <>
            <DataTable
              onRowClick={tx => setSelectedTx(tx)}
              columns={[
                { key:'date', header:'Date', cell:r=>fmtDate(r.postedDate||r.pendingDate) },
                { key:'desc', header:'Description', cell:r=>(
                  <div>
                    <div style={{fontWeight:500,color:'var(--text-primary)'}}>{r.description}</div>
                    {r.merchantCategory && <div style={{fontSize:'11px',color:'var(--text-secondary)'}}>{r.merchantCategory}</div>}
                  </div>
                )},
                { key:'type', header:'Type', cell:r=><TypeBadge tx={r} /> },
                { key:'status', header:'Status', cell:r=><StatusBadge tx={r} /> },
                { key:'amount', header:'Amount', align:'right', mono:true, cell:r=>(
                  <span style={{fontFamily:'JetBrains Mono,monospace', fontWeight:600,
                    color: r.transactionType==='credit' ? 'var(--status-success)' : 'var(--text-primary)' }}>
                    {r.transactionType==='credit'?'+':'-'}{fmt(r.amount)}
                  </span>
                )},
              ]}
              rows={pageTxs} emptyMessage="No transactions found for this period." />

            {/* Pagination */}
            {totalPages > 1 && (
              <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between',
                marginTop:14, padding:'0 4px' }}>
                <span style={{ fontSize:'12px', color:'var(--text-secondary)' }}>
                  {filtered.length} transaction{filtered.length!==1?'s':''} · Page {page} of {totalPages}
                </span>
                <div style={{ display:'flex', gap:6 }}>
                  <Button variant="secondary" size="sm" disabled={page<=1}
                    icon="ChevronLeft" onClick={()=>setPage(p=>Math.max(1,p-1))}>Prev</Button>
                  <Button variant="secondary" size="sm" disabled={page>=totalPages}
                    icon="ChevronRight" iconPos="suffix" onClick={()=>setPage(p=>Math.min(totalPages,p+1))}>Next</Button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
    <TxDrawer tx={selectedTx} onClose={() => setSelectedTx(null)} />
  );
}

// ── Holdings ──────────────────────────────────────────────────────────────────
function HoldingsPage({ accounts, onDisconnect }) {
  const [loading, setLoading] = useState(true);
  const [tab, setTab] = useState('overview'); // overview | pnl
  useEffect(() => { const t=setTimeout(()=>setLoading(false),1000); return ()=>clearTimeout(t); },[]);

  const totalNetWorth   = accounts.reduce((s,a)=>s+a.balanceUsd,0);
  const totalBanking    = accounts.filter(a=>a.category==='banking').reduce((s,a)=>s+a.balanceUsd,0);
  const totalBrokerage  = accounts.filter(a=>a.category==='brokerage').reduce((s,a)=>s+a.balanceUsd,0);
  const totalCrypto     = accounts.filter(a=>a.category==='crypto').reduce((s,a)=>s+a.balanceUsd,0);

  const CATEGORY_LABELS = { banking:'Banking', brokerage:'Brokerage & Investment', crypto:'Digital Assets' };
  const PROVIDER_CAN_DISCONNECT = { plaid:true, monobank:true, binance:true, ibkr:false };

  return (
    <div style={{ padding:24 }}>
      <div style={{ maxWidth:1200, margin:'0 auto', display:'flex', flexDirection:'column', gap:20 }}>
        {/* Header */}
        <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between' }}>
          <div>
            <h1 style={{ fontSize:'22px', fontWeight:700, color:'var(--text-primary)' }}>Asset Allocation</h1>
            <p style={{ fontSize:'13px', color:'var(--text-secondary)', marginTop:4 }}>
              Portfolio breakdown across all connected accounts
            </p>
          </div>
          <div style={{ display:'flex', gap:10 }}>
            <Button variant="secondary" size="sm" icon="Download">Export CSV</Button>
            <Button size="sm" icon="RefreshCw">Rebalance</Button>
          </div>
        </div>

        {/* Tab switcher */}
        <div style={{ display:'flex', gap:0, borderBottom:'2px solid var(--border-default)' }}>
          {[['overview','Overview'],['pnl','P&L / Positions']].map(([id, label]) => (
            <button key={id} onClick={() => setTab(id)}
              style={{ padding:'9px 20px', fontSize:'13px', fontWeight:500, background:'none',
                border:'none', cursor:'pointer', fontFamily:'inherit', transition:'color 120ms',
                color: tab===id ? 'var(--accent-default)' : 'var(--text-secondary)',
                borderBottom: `2px solid ${tab===id ? 'var(--accent-default)' : 'transparent'}`,
                marginBottom:'-2px' }}>
              {label}
            </button>
          ))}
        </div>

        {/* ── Overview tab ── */}
        {tab === 'overview' && <>
        {/* KPI cards */}
        <div style={{ display:'grid', gridTemplateColumns:'repeat(3,1fr)', gap:16 }}>
          <StatCard label="Net Asset Value" value={loading?'—':fmt(totalNetWorth)} icon="Wallet" loading={loading}
            delta={2.8} deltaLabel="30-day change" />
          <StatCard label="Banking" value={loading?'—':fmt(totalBanking)} icon="Building2" loading={loading} />
          <StatCard label="Brokerage & Crypto" value={loading?'—':fmt(totalBrokerage+totalCrypto)} icon="PieChart" loading={loading} />
        </div>

        {/* Allocation visual */}
        {!loading && totalNetWorth > 0 && (() => {
          const cats = [
            { label:'Banking', value:totalBanking, color:'#4f46e5' },
            { label:'Brokerage', value:totalBrokerage, color:'#10b981' },
            { label:'Crypto', value:totalCrypto, color:'#f59e0b' },
          ].filter(c=>c.value>0);
          return (
            <Card>
              <div style={{ fontSize:'11px', fontWeight:600, letterSpacing:'0.07em', textTransform:'uppercase',
                color:'var(--text-secondary)', marginBottom:12 }}>Allocation Overview</div>
              {/* Bar */}
              <div style={{ display:'flex', borderRadius:6, overflow:'hidden', height:20, marginBottom:16 }}>
                {cats.map((c,i)=>(
                  <div key={i} title={`${c.label}: ${((c.value/totalNetWorth)*100).toFixed(1)}%`}
                    style={{ width:`${(c.value/totalNetWorth)*100}%`, background:c.color,
                      transition:'width 400ms', cursor:'default' }} />
                ))}
              </div>
              <div style={{ display:'flex', gap:24 }}>
                {cats.map((c,i)=>(
                  <div key={i} style={{ display:'flex', alignItems:'center', gap:8 }}>
                    <div style={{ width:10, height:10, borderRadius:3, background:c.color }} />
                    <span style={{ fontSize:'12px', color:'var(--text-secondary)' }}>{c.label}</span>
                    <span style={{ fontSize:'12px', fontWeight:600, color:'var(--text-primary)',
                      fontFamily:'JetBrains Mono,monospace' }}>
                      {((c.value/totalNetWorth)*100).toFixed(1)}%
                    </span>
                  </div>
                ))}
              </div>
            </Card>
          );
        })()}

        {/* Detailed table */}
        {loading ? <Skeleton height={300} radius={8} /> : (
          <DataTable
            columns={[
              { key:'institution', header:'Institution', cell:r=>(
                <div style={{ display:'flex', alignItems:'center', gap:12 }}>
                  <InstitutionAvatar name={r.bankName} size={32} />
                  <div>
                    <div style={{ fontWeight:500, color:'var(--text-primary)' }}>{r.bankName}</div>
                    <div style={{ fontSize:'11px', color:'var(--text-secondary)' }}>
                      {r.accountNumberLast4 ? `•••• ${r.accountNumberLast4}` : r.provider}
                    </div>
                  </div>
                </div>
              )},
              { key:'type', header:'Type', cell:r=>r.accountType },
              { key:'category', header:'Category', cell:r=>(
                <Badge variant="neutral">{CATEGORY_LABELS[r.category]||r.category}</Badge>
              )},
              { key:'status', header:'Status', cell:r=><StatusIndicator status={r.syncStatus} /> },
              { key:'balance', header:'Balance', align:'right', mono:true, cell:r=>(
                <div style={{ textAlign:'right' }}>
                  <div style={{ fontFamily:'JetBrains Mono,monospace', fontWeight:500 }}>
                    {r.currency!=='USD' ? `${fmtNum(r.currentBalance)} ${r.currency}` : fmt(r.currentBalance)}
                  </div>
                  {r.currency!=='USD' && <div style={{ fontSize:'11px', color:'var(--text-disabled)' }}>≈ {fmt(r.balanceUsd)}</div>}
                </div>
              )},
              { key:'usdValue', header:'USD Value', align:'right', mono:true, cell:r=>(
                <span style={{ fontFamily:'JetBrains Mono,monospace', fontWeight:600, color:'var(--text-primary)' }}>
                  {fmt(r.balanceUsd)}
                </span>
              )},
              { key:'pct', header:'Portfolio %', align:'right', cell:r=>(
                <span style={{ fontFamily:'JetBrains Mono,monospace', fontSize:'12px', color:'var(--text-secondary)' }}>
                  {((r.balanceUsd/totalNetWorth)*100).toFixed(1)}%
                </span>
              )},
              { key:'actions', header:'', cell:r=>(
                PROVIDER_CAN_DISCONNECT[r.provider]
                  ? <Button variant="secondary" size="sm" onClick={()=>onDisconnect(r)}>Disconnect</Button>
                  : null
              )},
            ]}
            rows={accounts} />
        )}
        </> /* end overview tab */}

        {/* ── P&L tab ── */}
        {tab === 'pnl' && (() => {
          const positions = PORTFOLIO_HOLDINGS;
          const totalCost   = positions.reduce((s,p) => s + p.qty * p.avgCost, 0);
          const totalValue  = positions.reduce((s,p) => s + p.qty * p.currentPrice, 0);
          const totalPnl    = totalValue - totalCost;
          const totalPct    = totalCost > 0 ? (totalPnl / totalCost) * 100 : 0;

          return (
            <div style={{ display:'flex', flexDirection:'column', gap:16 }}>
              {/* P&L summary */}
              <div style={{ display:'grid', gridTemplateColumns:'repeat(3,1fr)', gap:14 }}>
                {[
                  ['Total Cost Basis', fmt(totalCost),  'var(--text-primary)'],
                  ['Current Value',    fmt(totalValue), 'var(--text-primary)'],
                  ['Unrealized P&L',   `${totalPnl>=0?'+':''}${fmt(totalPnl)} (${totalPct>=0?'+':''}${totalPct.toFixed(2)}%)`,
                   totalPnl >= 0 ? 'var(--status-success)' : 'var(--status-error)'],
                ].map(([label, value, color]) => (
                  <div key={label} style={{ background:'var(--surface-card)', border:'1px solid var(--border-default)',
                    borderRadius:8, padding:'14px 18px' }}>
                    <div style={{ fontSize:'11px', textTransform:'uppercase', letterSpacing:'0.06em',
                      color:'var(--text-secondary)', marginBottom:6 }}>{label}</div>
                    <div style={{ fontFamily:'JetBrains Mono,monospace', fontSize:'18px', fontWeight:700, color }}>{value}</div>
                  </div>
                ))}
              </div>

              {/* Positions table */}
              <DataTable
                columns={[
                  { key:'symbol', header:'Symbol', cell:r=>(
                    <div>
                      <div style={{ fontWeight:700, color:'var(--text-primary)', fontFamily:'JetBrains Mono,monospace' }}>{r.symbol}</div>
                      <div style={{ fontSize:'11px', color:'var(--text-secondary)' }}>{r.name}</div>
                    </div>
                  )},
                  { key:'qty', header:'Qty', align:'right', mono:true, cell:r=>(
                    <span style={{ fontFamily:'JetBrains Mono,monospace' }}>{r.qty}</span>
                  )},
                  { key:'avg', header:'Avg Cost', align:'right', mono:true, cell:r=>(
                    <span style={{ fontFamily:'JetBrains Mono,monospace' }}>{fmt(r.avgCost)}</span>
                  )},
                  { key:'price', header:'Current Price', align:'right', mono:true, cell:r=>(
                    <span style={{ fontFamily:'JetBrains Mono,monospace' }}>{fmt(r.currentPrice)}</span>
                  )},
                  { key:'value', header:'Market Value', align:'right', mono:true, cell:r=>(
                    <span style={{ fontFamily:'JetBrains Mono,monospace', fontWeight:600 }}>{fmt(r.qty * r.currentPrice)}</span>
                  )},
                  { key:'pnl', header:'Unrealized P&L', align:'right', cell:r=>{
                    const pnl = (r.currentPrice - r.avgCost) * r.qty;
                    const pct = ((r.currentPrice - r.avgCost) / r.avgCost) * 100;
                    const pos = pnl >= 0;
                    return (
                      <div style={{ textAlign:'right' }}>
                        <div style={{ fontFamily:'JetBrains Mono,monospace', fontWeight:600,
                          color: pos ? 'var(--status-success)' : 'var(--status-error)' }}>
                          {pos?'+':''}{fmt(pnl)}
                        </div>
                        <div style={{ fontSize:'11px', color: pos ? 'var(--status-success)' : 'var(--status-error)' }}>
                          {pos?'+':''}{pct.toFixed(2)}%
                        </div>
                      </div>
                    );
                  }},
                ]}
                rows={positions} />
            </div>
          );
        })()}
      </div>
    </div>
  );
}

Object.assign(window, { DashboardPage, AccountsListPage, TransactionListPage, HoldingsPage });
