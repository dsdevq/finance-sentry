// Finance Sentry — Alerts Page
const { useState, useMemo } = React;

const ALERT_META = {
  sync_error:    { icon:'AlertCircle',   color:'var(--status-error)',   bg:'rgba(239,68,68,.08)',   label:'Sync Error'   },
  low_balance:   { icon:'AlertTriangle', color:'var(--status-warning)', bg:'rgba(245,158,11,.08)',  label:'Low Balance'  },
  unusual_spend: { icon:'Zap',           color:'var(--status-warning)', bg:'rgba(245,158,11,.08)',  label:'Unusual Spend'},
  budget:        { icon:'TrendingDown',  color:'var(--status-warning)', bg:'rgba(245,158,11,.08)',  label:'Budget'       },
  info:          { icon:'Info',          color:'var(--status-info)',    bg:'rgba(99,102,241,.08)',  label:'Info'         },
};

function AlertsPage() {
  const { addToast } = useToast();
  const [alerts, setAlerts]   = useState(ALERTS);
  const [filter, setFilter]   = useState('all'); // all | unread | error | warning | info

  const unreadCount = alerts.filter(a => !a.read).length;

  const markRead   = id  => setAlerts(as => as.map(a => a.id === id ? {...a, read:true} : a));
  const dismiss    = id  => { setAlerts(as => as.filter(a => a.id !== id)); addToast('Alert dismissed', 'info'); };
  const markAllRead = () => { setAlerts(as => as.map(a => ({...a, read:true}))); addToast('All alerts marked as read', 'success'); };
  const clearAll   = () => { setAlerts([]); addToast('All alerts cleared', 'info'); };

  const filtered = useMemo(() => {
    if (filter === 'unread')  return alerts.filter(a => !a.read);
    if (filter === 'error')   return alerts.filter(a => a.severity === 'error');
    if (filter === 'warning') return alerts.filter(a => a.severity === 'warning');
    if (filter === 'info')    return alerts.filter(a => a.severity === 'info');
    return alerts;
  }, [alerts, filter]);

  const errorCount   = alerts.filter(a => a.severity === 'error').length;
  const warningCount = alerts.filter(a => a.severity === 'warning').length;

  return (
    <div style={{ padding:24 }}>
      <div style={{ maxWidth:860, margin:'0 auto', display:'flex', flexDirection:'column', gap:20 }}>

        {/* Header */}
        <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between' }}>
          <div>
            <h1 style={{ fontSize:'22px', fontWeight:700, color:'var(--text-primary)' }}>Alerts</h1>
            <p style={{ fontSize:'13px', color:'var(--text-secondary)', marginTop:4 }}>
              {unreadCount > 0 ? `${unreadCount} unread alert${unreadCount > 1 ? 's' : ''}` : 'All caught up'}
            </p>
          </div>
          <div style={{ display:'flex', gap:8 }}>
            {unreadCount > 0 && (
              <Button variant="secondary" size="sm" icon="CheckCheck" onClick={markAllRead}>
                Mark all read
              </Button>
            )}
            {alerts.length > 0 && (
              <Button variant="ghost" size="sm" onClick={clearAll}>Clear all</Button>
            )}
          </div>
        </div>

        {/* Summary chips */}
        <div style={{ display:'flex', gap:10, flexWrap:'wrap' }}>
          {[
            ['all',     `All (${alerts.length})`,       'neutral'],
            ['unread',  `Unread (${unreadCount})`,       unreadCount > 0 ? 'info' : 'neutral'],
            ['error',   `Errors (${errorCount})`,        errorCount > 0  ? 'error' : 'neutral'],
            ['warning', `Warnings (${warningCount})`,    warningCount > 0? 'warning': 'neutral'],
            ['info',    'Info',                          'neutral'],
          ].map(([id, label, variant]) => (
            <button key={id} onClick={() => setFilter(id)}
              style={{ padding:'5px 12px', borderRadius:20, fontSize:'12px', fontWeight:500,
                border:'1.5px solid', cursor:'pointer', fontFamily:'inherit', transition:'all 120ms',
                borderColor: filter === id ? 'var(--accent-default)' : 'var(--border-default)',
                background:  filter === id ? 'var(--accent-subtle)'  : 'transparent',
                color:       filter === id ? 'var(--accent-default)' : 'var(--text-secondary)' }}>
              {label}
            </button>
          ))}
        </div>

        {/* Alert list */}
        {filtered.length === 0 ? (
          <Card style={{ padding:'48px', textAlign:'center' }}>
            <Icon name="CheckCircle2" size="xl" style={{ color:'var(--status-success)', margin:'0 auto 14px', display:'block' }} />
            <div style={{ fontSize:'15px', fontWeight:600, color:'var(--text-primary)', marginBottom:6 }}>
              {filter === 'all' ? 'No alerts' : `No ${filter} alerts`}
            </div>
            <div style={{ fontSize:'13px', color:'var(--text-secondary)' }}>
              {filter === 'all' ? 'All your accounts are healthy.' : 'Try a different filter.'}
            </div>
          </Card>
        ) : (
          <div style={{ display:'flex', flexDirection:'column', gap:10 }}>
            {filtered.map(alert => {
              const meta = ALERT_META[alert.type] || ALERT_META.info;
              return (
                <div key={alert.id}
                  onClick={() => !alert.read && markRead(alert.id)}
                  style={{ display:'flex', alignItems:'flex-start', gap:14, padding:'16px 18px',
                    background: alert.read ? 'var(--surface-card)' : meta.bg,
                    border:`1px solid ${alert.read ? 'var(--border-default)' : meta.color + '30'}`,
                    borderRadius:10, cursor: alert.read ? 'default' : 'pointer',
                    transition:'background 150ms, border-color 150ms', position:'relative' }}>

                  {/* Unread dot */}
                  {!alert.read && (
                    <div style={{ position:'absolute', top:14, right:14, width:8, height:8,
                      borderRadius:'50%', background: meta.color }} />
                  )}

                  {/* Icon */}
                  <div style={{ width:38, height:38, borderRadius:10, background:`${meta.color}18`,
                    display:'flex', alignItems:'center', justifyContent:'center', flexShrink:0 }}>
                    <Icon name={meta.icon} size="sm" style={{ color:meta.color }} />
                  </div>

                  {/* Content */}
                  <div style={{ flex:1, minWidth:0 }}>
                    <div style={{ display:'flex', alignItems:'center', gap:8, marginBottom:5, flexWrap:'wrap' }}>
                      <span style={{ fontSize:'14px', fontWeight:600,
                        color: alert.read ? 'var(--text-primary)' : meta.color }}>
                        {alert.title}
                      </span>
                      <Badge variant={alert.severity === 'error' ? 'error' : alert.severity === 'warning' ? 'warning' : 'info'}>
                        {meta.label}
                      </Badge>
                      {alert.account && (
                        <span style={{ fontSize:'11px', color:'var(--text-disabled)',
                          background:'var(--surface-raised)', padding:'2px 7px', borderRadius:4 }}>
                          {alert.account}
                        </span>
                      )}
                    </div>
                    <p style={{ fontSize:'13px', color:'var(--text-secondary)', lineHeight:1.55,
                      margin:0, marginBottom:8 }}>{alert.body}</p>
                    <span style={{ fontSize:'11px', color:'var(--text-disabled)' }}>
                      {relativeTime(alert.ts)}
                    </span>
                  </div>

                  {/* Dismiss */}
                  <button onClick={e => { e.stopPropagation(); dismiss(alert.id); }}
                    style={{ background:'none', border:'none', padding:4, cursor:'pointer',
                      color:'var(--text-disabled)', borderRadius:5, display:'flex', flexShrink:0,
                      transition:'color 120ms, background 120ms' }}
                    onMouseEnter={e => { e.currentTarget.style.color='var(--text-primary)'; e.currentTarget.style.background='var(--surface-raised)'; }}
                    onMouseLeave={e => { e.currentTarget.style.color='var(--text-disabled)'; e.currentTarget.style.background='none'; }}>
                    <Icon name="X" size="xs" />
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

Object.assign(window, { AlertsPage });
