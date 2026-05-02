// Finance Sentry — Budget + Subscriptions Pages
const { useState, useMemo } = React;

// ── Budget Page ───────────────────────────────────────────────────────────────
function BudgetPage() {
  const { addToast } = useToast();
  const [budgets, setBudgets] = useState(BUDGETS.map(b => ({ ...b })));
  const [editing, setEditing] = useState(null); // category string
  const [editVal, setEditVal] = useState('');

  const totalSpent  = budgets.reduce((s, b) => s + b.spent, 0);
  const totalBudget = budgets.reduce((s, b) => s + b.limit, 0);
  const overCount   = budgets.filter(b => b.spent > b.limit).length;

  const startEdit = (b) => { setEditing(b.category); setEditVal(String(b.limit)); };
  const saveEdit  = () => {
    const val = parseFloat(editVal);
    if (!isNaN(val) && val > 0) {
      setBudgets(bs => bs.map(b => b.category === editing ? { ...b, limit: val } : b));
      addToast(`Budget for ${editing} updated`, 'success');
    }
    setEditing(null);
  };

  return (
    <div style={{ padding:24 }}>
      <div style={{ maxWidth:1000, margin:'0 auto', display:'flex', flexDirection:'column', gap:20 }}>

        {/* Header */}
        <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between' }}>
          <div>
            <h1 style={{ fontSize:'22px', fontWeight:700, color:'var(--text-primary)' }}>Budgets</h1>
            <p style={{ fontSize:'13px', color:'var(--text-secondary)', marginTop:4 }}>
              Monthly spending limits · April 2026
            </p>
          </div>
          <div style={{ textAlign:'right' }}>
            <div style={{ fontSize:'11px', textTransform:'uppercase', letterSpacing:'0.06em',
              color:'var(--text-secondary)', marginBottom:4 }}>Total Spent vs Budget</div>
            <div style={{ fontFamily:'JetBrains Mono,monospace', fontSize:'20px', fontWeight:700,
              color: totalSpent > totalBudget ? 'var(--status-error)' : 'var(--text-primary)' }}>
              {fmt(totalSpent)} <span style={{ fontSize:'14px', color:'var(--text-secondary)', fontWeight:400 }}>/ {fmt(totalBudget)}</span>
            </div>
          </div>
        </div>

        {/* Alert if over budget */}
        {overCount > 0 && (
          <Alert variant="warning">
            {overCount} categor{overCount > 1 ? 'ies are' : 'y is'} over budget this month.
            Review your spending or adjust your limits below.
          </Alert>
        )}

        {/* Overall progress bar */}
        <Card style={{ padding:'18px 20px' }}>
          <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between', marginBottom:10 }}>
            <span style={{ fontSize:'12px', fontWeight:600, color:'var(--text-secondary)',
              letterSpacing:'0.05em', textTransform:'uppercase' }}>Overall</span>
            <span style={{ fontSize:'13px', fontWeight:600, fontFamily:'JetBrains Mono,monospace',
              color: totalSpent > totalBudget ? 'var(--status-error)' : 'var(--text-primary)' }}>
              {((totalSpent / totalBudget) * 100).toFixed(0)}%
            </span>
          </div>
          <div style={{ height:10, borderRadius:6, background:'var(--surface-raised)', overflow:'hidden' }}>
            <div style={{ height:'100%', borderRadius:6, transition:'width 600ms ease',
              width:`${Math.min((totalSpent / totalBudget) * 100, 100)}%`,
              background: totalSpent > totalBudget ? 'var(--status-error)' : 'var(--accent-default)' }} />
          </div>
          {/* Per-category mini bars stacked */}
          <div style={{ display:'flex', height:5, borderRadius:4, overflow:'hidden', marginTop:8, gap:1 }}>
            {budgets.map(b => (
              <div key={b.category} title={`${b.category}: ${fmt(b.spent)}`}
                style={{ flex: b.spent, background: b.color, transition:'flex 400ms', minWidth:2 }} />
            ))}
          </div>
          <div style={{ display:'flex', flexWrap:'wrap', gap:'6px 16px', marginTop:10 }}>
            {budgets.map(b => (
              <div key={b.category} style={{ display:'flex', alignItems:'center', gap:5 }}>
                <div style={{ width:8, height:8, borderRadius:2, background:b.color }} />
                <span style={{ fontSize:'11px', color:'var(--text-secondary)' }}>{b.category}</span>
              </div>
            ))}
          </div>
        </Card>

        {/* Individual category cards */}
        <div style={{ display:'grid', gridTemplateColumns:'repeat(auto-fill, minmax(280px, 1fr))', gap:14 }}>
          {budgets.map(b => {
            const pct     = Math.min((b.spent / b.limit) * 100, 100);
            const over    = b.spent > b.limit;
            const nearOver = !over && pct >= 80;
            const barColor = over ? 'var(--status-error)' : nearOver ? 'var(--status-warning)' : b.color;
            const remaining = b.limit - b.spent;
            const isEditing = editing === b.category;

            return (
              <Card key={b.category} style={{ padding:'16px 18px' }}>
                <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between', marginBottom:12 }}>
                  <div style={{ display:'flex', alignItems:'center', gap:10 }}>
                    <div style={{ width:10, height:10, borderRadius:3, background:b.color, flexShrink:0 }} />
                    <span style={{ fontSize:'13px', fontWeight:600, color:'var(--text-primary)' }}>
                      {b.category}
                    </span>
                  </div>
                  {over   && <Badge variant="error">Over</Badge>}
                  {nearOver && !over && <Badge variant="warning">Near limit</Badge>}
                </div>

                {/* Progress bar */}
                <div style={{ height:7, borderRadius:4, background:'var(--surface-raised)',
                  overflow:'hidden', marginBottom:10 }}>
                  <div style={{ height:'100%', borderRadius:4, width:`${pct}%`,
                    background:barColor, transition:'width 500ms ease' }} />
                </div>

                {/* Amounts row */}
                <div style={{ display:'flex', alignItems:'baseline', justifyContent:'space-between', marginBottom:6 }}>
                  <span style={{ fontFamily:'JetBrains Mono,monospace', fontSize:'16px', fontWeight:700,
                    color: over ? 'var(--status-error)' : 'var(--text-primary)' }}>
                    {fmt(b.spent)}
                  </span>
                  {isEditing ? (
                    <div style={{ display:'flex', alignItems:'center', gap:6 }}>
                      <span style={{ fontSize:'11px', color:'var(--text-secondary)' }}>/ $</span>
                      <input autoFocus value={editVal} onChange={e=>setEditVal(e.target.value)}
                        onBlur={saveEdit} onKeyDown={e=>{ if(e.key==='Enter') saveEdit(); if(e.key==='Escape') setEditing(null); }}
                        style={{ width:72, padding:'3px 7px', borderRadius:5, border:'1.5px solid var(--accent-default)',
                          background:'var(--surface-bg)', color:'var(--text-primary)', fontSize:'13px',
                          outline:'none', fontFamily:'JetBrains Mono,monospace', textAlign:'right' }} />
                    </div>
                  ) : (
                    <button onClick={() => startEdit(b)}
                      style={{ fontSize:'12px', color:'var(--text-disabled)', background:'none', border:'none',
                        cursor:'pointer', fontFamily:'JetBrains Mono,monospace', padding:'2px 4px',
                        borderRadius:4, transition:'color 120ms, background 120ms' }}
                      onMouseEnter={e=>{e.currentTarget.style.color='var(--accent-default)';e.currentTarget.style.background='var(--accent-subtle)'}}
                      onMouseLeave={e=>{e.currentTarget.style.color='var(--text-disabled)';e.currentTarget.style.background='none'}}>
                      / {fmt(b.limit)} ✎
                    </button>
                  )}
                </div>

                <div style={{ fontSize:'12px', color: over ? 'var(--status-error)' : 'var(--text-secondary)' }}>
                  {over
                    ? `${fmt(Math.abs(remaining))} over budget`
                    : `${fmt(remaining)} remaining · ${pct.toFixed(0)}% used`}
                </div>
              </Card>
            );
          })}
        </div>
      </div>
    </div>
  );
}

// ── Subscriptions Page ────────────────────────────────────────────────────────
function SubscriptionsPage() {
  const { addToast } = useToast();
  const [subs, setSubs] = useState(SUBSCRIPTIONS.map(s => ({ ...s })));
  const [cancelTarget, setCancelTarget] = useState(null);
  const [sort, setSort] = useState('date'); // date | amount | name

  const toggle = (id) => {
    setSubs(ss => ss.map(s => s.id === id ? { ...s, status: s.status === 'active' ? 'paused' : 'active' } : s));
    const sub = subs.find(s => s.id === id);
    addToast(`${sub.name} ${sub.status === 'active' ? 'paused' : 'resumed'}`, 'info');
  };

  const remove = (id) => {
    setSubs(ss => ss.filter(s => s.id !== id));
    addToast(`${cancelTarget.name} cancelled`, 'warning');
    setCancelTarget(null);
  };

  const active = subs.filter(s => s.status === 'active');
  const paused = subs.filter(s => s.status === 'paused');
  const monthlyTotal  = active.reduce((s, sub) => s + sub.amount, 0);
  const yearlyTotal   = monthlyTotal * 12;

  const sorted = (arr) => [...arr].sort((a, b) => {
    if (sort === 'amount') return b.amount - a.amount;
    if (sort === 'name')   return a.name.localeCompare(b.name);
    return a.nextDate.localeCompare(b.nextDate); // date
  });

  const daysUntil = dateStr => {
    const d = new Date(dateStr) - new Date();
    return Math.ceil(d / 864e5);
  };

  const SubRow = ({ sub }) => {
    const days = daysUntil(sub.nextDate);
    const soon = days <= 5;
    return (
      <div style={{ display:'flex', alignItems:'center', gap:14, padding:'14px 16px',
        borderBottom:'1px solid var(--border-default)', opacity: sub.status === 'paused' ? 0.55 : 1,
        transition:'opacity 150ms' }}
        onMouseEnter={e=>e.currentTarget.style.background='var(--surface-bg)'}
        onMouseLeave={e=>e.currentTarget.style.background=''}>
        {/* Logo */}
        <div style={{ width:40, height:40, borderRadius:10, background:sub.color,
          display:'flex', alignItems:'center', justifyContent:'center',
          fontSize:'15px', fontWeight:800, color:'white', flexShrink:0 }}>{sub.logo}</div>

        {/* Name + category */}
        <div style={{ flex:1, minWidth:0 }}>
          <div style={{ fontSize:'14px', fontWeight:500, color:'var(--text-primary)', marginBottom:2 }}>{sub.name}</div>
          <div style={{ fontSize:'12px', color:'var(--text-secondary)' }}>{sub.category}</div>
        </div>

        {/* Next charge */}
        <div style={{ textAlign:'center', minWidth:100 }}>
          <div style={{ fontSize:'12px', color: soon ? 'var(--status-warning)' : 'var(--text-secondary)', fontWeight: soon ? 600 : 400 }}>
            {fmtDate(sub.nextDate)}
          </div>
          <div style={{ fontSize:'11px', color: soon ? 'var(--status-warning)' : 'var(--text-disabled)' }}>
            {days <= 0 ? 'Today' : `in ${days}d`}
          </div>
        </div>

        {/* Amount */}
        <div style={{ fontFamily:'JetBrains Mono,monospace', fontSize:'15px', fontWeight:600,
          color:'var(--text-primary)', minWidth:72, textAlign:'right' }}>
          {fmt(sub.amount)}
          <div style={{ fontSize:'10px', fontWeight:400, color:'var(--text-disabled)', textAlign:'right' }}>/ mo</div>
        </div>

        {/* Actions */}
        <div style={{ display:'flex', gap:6, flexShrink:0 }}>
          <Button variant="secondary" size="sm" onClick={() => toggle(sub.id)}>
            {sub.status === 'active' ? 'Pause' : 'Resume'}
          </Button>
          <Button variant="ghost" size="sm" icon="X" onClick={() => setCancelTarget(sub)} />
        </div>
      </div>
    );
  };

  return (
    <div style={{ padding:24 }}>
      <div style={{ maxWidth:900, margin:'0 auto', display:'flex', flexDirection:'column', gap:20 }}>

        {/* Header */}
        <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between' }}>
          <div>
            <h1 style={{ fontSize:'22px', fontWeight:700, color:'var(--text-primary)' }}>Subscriptions</h1>
            <p style={{ fontSize:'13px', color:'var(--text-secondary)', marginTop:4 }}>
              Recurring charges detected from your transactions
            </p>
          </div>
        </div>

        {/* Summary cards */}
        <div style={{ display:'grid', gridTemplateColumns:'repeat(3,1fr)', gap:14 }}>
          {[
            ['Monthly Cost',  fmt(monthlyTotal),   'Zap',       'var(--accent-default)'],
            ['Annual Cost',   fmt(yearlyTotal),     'TrendingUp','var(--status-error)'],
            ['Active',        `${active.length} subscriptions`, 'CheckCheck', 'var(--status-success)'],
          ].map(([label, value, icon, color]) => (
            <Card key={label} style={{ padding:'16px 18px', display:'flex', alignItems:'center', gap:14 }}>
              <div style={{ width:40, height:40, borderRadius:10, background:`${color}18`,
                display:'flex', alignItems:'center', justifyContent:'center', flexShrink:0 }}>
                <Icon name={icon} size="sm" style={{ color }} />
              </div>
              <div>
                <div style={{ fontSize:'11px', textTransform:'uppercase', letterSpacing:'0.06em',
                  color:'var(--text-secondary)', marginBottom:4 }}>{label}</div>
                <div style={{ fontFamily:'JetBrains Mono,monospace', fontSize:'17px', fontWeight:700,
                  color:'var(--text-primary)' }}>{value}</div>
              </div>
            </Card>
          ))}
        </div>

        {/* Sort controls */}
        <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between' }}>
          <span style={{ fontSize:'12px', fontWeight:600, textTransform:'uppercase',
            letterSpacing:'0.07em', color:'var(--text-secondary)' }}>
            Active · {active.length}
          </span>
          <div style={{ display:'flex', gap:4 }}>
            {[['date','Next charge'],['amount','Amount'],['name','Name']].map(([v,l]) => (
              <button key={v} onClick={() => setSort(v)}
                style={{ padding:'4px 10px', borderRadius:6, fontSize:'12px', fontWeight:500,
                  border:'1.5px solid', cursor:'pointer', fontFamily:'inherit', transition:'all 120ms',
                  borderColor: sort===v ? 'var(--accent-default)' : 'var(--border-default)',
                  background:  sort===v ? 'var(--accent-subtle)'  : 'transparent',
                  color:       sort===v ? 'var(--accent-default)' : 'var(--text-secondary)' }}>
                {l}
              </button>
            ))}
          </div>
        </div>

        {/* Active subscriptions */}
        <Card padding="none">
          {sorted(active).map(sub => <SubRow key={sub.id} sub={sub} />)}
          {active.length === 0 && (
            <div style={{ padding:'32px', textAlign:'center', color:'var(--text-disabled)', fontSize:'13px' }}>
              No active subscriptions
            </div>
          )}
        </Card>

        {/* Paused */}
        {paused.length > 0 && (
          <>
            <span style={{ fontSize:'12px', fontWeight:600, textTransform:'uppercase',
              letterSpacing:'0.07em', color:'var(--text-secondary)' }}>Paused · {paused.length}</span>
            <Card padding="none">
              {sorted(paused).map(sub => <SubRow key={sub.id} sub={sub} />)}
            </Card>
          </>
        )}
      </div>

      <ConfirmDialog open={!!cancelTarget} onClose={() => setCancelTarget(null)}
        onConfirm={() => remove(cancelTarget?.id)}
        confirmVariant="destructive" confirmLabel="Cancel subscription"
        title={`Cancel ${cancelTarget?.name}?`}
        message={`This will remove ${cancelTarget?.name} from your tracked subscriptions. Your actual subscription with the provider won't be affected.`} />
    </div>
  );
}

Object.assign(window, { BudgetPage, SubscriptionsPage });
