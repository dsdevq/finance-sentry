// Finance Sentry — UI Components
const { useState, useRef, useMemo } = React;

// ── Icons ────────────────────────────────────────────────────────────────────
const ICONS = {
  LayoutDashboard: <><rect width="7" height="9" x="3" y="3" rx="1"/><rect width="7" height="5" x="14" y="3" rx="1"/><rect width="7" height="9" x="14" y="12" rx="1"/><rect width="7" height="5" x="3" y="16" rx="1"/></>,
  CreditCard: <><rect width="22" height="16" x="1" y="4" rx="2" ry="2"/><line x1="1" x2="23" y1="10" y2="10"/></>,
  ArrowLeftRight: <><path d="M8 3 4 7l4 4"/><path d="M4 7h16"/><path d="m16 21 4-4-4-4"/><path d="M20 17H4"/></>,
  PieChart: <><path d="M21.21 15.89A10 10 0 1 1 8 2.83"/><path d="M22 12A10 10 0 0 0 12 2v10z"/></>,
  Wallet: <><path d="M21 12V7H5a2 2 0 0 1 0-4h14v4"/><path d="M3 5v14a2 2 0 0 0 2 2h16v-5"/><path d="M18 12a2 2 0 0 0 0 4h4v-4z"/></>,
  Building2: <><path d="M6 22V4a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v18H6z"/><path d="M6 12H4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h2"/><path d="M18 9h2a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2h-2"/><path d="M10 6h4"/><path d="M10 10h4"/><path d="M10 14h4"/><path d="M10 18h4"/></>,
  TrendingUp: <><polyline points="22 7 13.5 15.5 8.5 10.5 2 17"/><polyline points="16 7 22 7 22 13"/></>,
  TrendingDown: <><polyline points="22 17 13.5 8.5 8.5 13.5 2 7"/><polyline points="16 17 22 17 22 11"/></>,
  Search: <><circle cx="11" cy="11" r="8"/><line x1="21" x2="16.65" y1="21" y2="16.65"/></>,
  Moon: <path d="M12 3a6.364 6.364 0 0 0 9 9 9 9 0 1 1-9-9Z"/>,
  Sun: <><circle cx="12" cy="12" r="4"/><path d="M12 2v2"/><path d="M12 20v2"/><path d="m4.93 4.93 1.41 1.41"/><path d="m17.66 17.66 1.41 1.41"/><path d="M2 12h2"/><path d="M20 12h2"/><path d="m6.34 17.66-1.41 1.41"/><path d="m19.07 4.93-1.41 1.41"/></>,
  PanelLeftClose: <><rect width="18" height="18" x="3" y="3" rx="2"/><path d="M9 3v18"/><path d="m16 15-3-3 3-3"/></>,
  PanelLeftOpen: <><rect width="18" height="18" x="3" y="3" rx="2"/><path d="M9 3v18"/><path d="m14 9 3 3-3 3"/></>,
  X: <><path d="M18 6 6 18"/><path d="m6 6 12 12"/></>,
  AlertCircle: <><circle cx="12" cy="12" r="10"/><line x1="12" x2="12" y1="8" y2="12"/><line x1="12" x2="12.01" y1="16" y2="16"/></>,
  CheckCircle2: <><path d="M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10z"/><path d="m9 12 2 2 4-4"/></>,
  Info: <><circle cx="12" cy="12" r="10"/><path d="M12 16v-4"/><path d="M12 8h.01"/></>,
  AlertTriangle: <><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3z"/><line x1="12" x2="12" y1="9" y2="13"/><line x1="12" x2="12.01" y1="17" y2="17"/></>,
  ChevronLeft: <path d="m15 18-6-6 6-6"/>,
  ChevronRight: <path d="m9 18 6-6-6-6"/>,
  ChevronDown: <path d="m6 9 6 6 6-6"/>,
  Plus: <><path d="M5 12h14"/><path d="M12 5v14"/></>,
  LogOut: <><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" x2="9" y1="12" y2="12"/></>,
  Loader2: <path d="M21 12a9 9 0 1 1-6.219-8.56"/>,
  RefreshCw: <><path d="M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8"/><path d="M21 3v5h-5"/><path d="M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16"/><path d="M8 16H3v5"/></>,
  Unplug: <><path d="m19 5-7 7"/><path d="m5 19 7-7"/><path d="M6 17c-1.1-1.1-1.1-2.9 0-4l1.5-1.5"/><path d="M10 13c1.1 1.1 1.1 2.9 0 4L8.5 18.5"/><path d="M18 6c1.1 1.1 1.1 2.9 0 4l-1.5 1.5"/><path d="M14 10c-1.1-1.1-1.1-2.9 0-4l1.5-1.5"/></>,
  Link: <><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></>,
  Download: <><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" x2="12" y1="15" y2="3"/></>,
  Filter: <><polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"/></>,
  Key: <><circle cx="7.5" cy="15.5" r="5.5"/><path d="m21 2-9.6 9.6"/><path d="m15.5 7.5 3 3L22 7l-3-3"/></>,
  ShieldCheck: <><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/><path d="m9 12 2 2 4-4"/></>,
  Eye: <><path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/><circle cx="12" cy="12" r="3"/></>,
  EyeOff: <><path d="M9.88 9.88a3 3 0 1 0 4.24 4.24"/><path d="M10.73 5.08A10.43 10.43 0 0 1 12 5c7 0 10 7 10 7a13.16 13.16 0 0 1-1.67 2.68"/><path d="M6.61 6.61A13.526 13.526 0 0 0 2 12s3 7 10 7a9.74 9.74 0 0 0 5.39-1.61"/><line x1="2" x2="22" y1="2" y2="22"/></>,
  CheckCheck: <><path d="M18 6 7 17l-5-5"/><path d="m22 10-7.5 7.5L13 16"/></>,
  Zap: <><polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/></>,
};

function Icon({ name, size = 'md', style: s = {}, className = '' }) {
  const content = ICONS[name];
  if (!content) return null;
  const sz = { xs: 12, sm: 14, md: 18, lg: 22, xl: 28 }[size] || 18;
  return (
    <svg width={sz} height={sz} viewBox="0 0 24 24" fill="none" stroke="currentColor"
      strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
      style={{ flexShrink: 0, ...s }} className={className} aria-hidden="true">
      {content}
    </svg>
  );
}

// ── Button ───────────────────────────────────────────────────────────────────
function Button({ children, variant='primary', size='md', disabled=false, loading=false,
    onClick, type='button', icon, iconPos='prefix', fullWidth=false, style:extra={} }) {
  const [hov, setHov] = useState(false);
  const pad = { sm:'5px 10px', md:'8px 16px', lg:'11px 22px' }[size];
  const fs  = { sm:'12px', md:'14px', lg:'15px' }[size];
  const gap = { sm:'5px', md:'7px', lg:'8px' }[size];
  const iconSz = size === 'lg' ? 'md' : 'sm';
  const variantStyle = {
    primary: { background: hov && !disabled ? 'var(--accent-hover)' : 'var(--accent-default)', color: 'var(--text-inverse)', border: '1.5px solid transparent' },
    secondary: { background: hov && !disabled ? 'var(--surface-raised)' : 'transparent', color: 'var(--text-primary)', border: '1.5px solid var(--border-default)' },
    destructive: { background: hov && !disabled ? '#dc2626' : 'var(--status-error)', color: '#fff', border: '1.5px solid transparent' },
    ghost: { background: hov && !disabled ? 'var(--surface-raised)' : 'transparent', color: 'var(--text-secondary)', border: '1.5px solid transparent' },
  }[variant] || {};
  return (
    <button type={type} disabled={disabled || loading} onClick={onClick}
      onMouseEnter={() => setHov(true)} onMouseLeave={() => setHov(false)}
      style={{ display:'inline-flex', alignItems:'center', justifyContent:'center', gap,
        padding:pad, fontSize:fs, fontWeight:500, fontFamily:'inherit', borderRadius:'6px',
        cursor: disabled||loading ? 'not-allowed':'pointer', opacity: disabled||loading ? 0.5:1,
        transition:'background 120ms, border-color 120ms, opacity 120ms', outline:'none',
        width: fullWidth?'100%':undefined, ...variantStyle, ...extra }}>
      {loading ? <Icon name="Loader2" size={iconSz} style={{animation:'spin 0.8s linear infinite'}} />
                : (icon && iconPos==='prefix' ? <Icon name={icon} size={iconSz} /> : null)}
      {children}
      {!loading && icon && iconPos==='suffix' ? <Icon name={icon} size={iconSz} /> : null}
    </button>
  );
}

// ── Badge ────────────────────────────────────────────────────────────────────
function Badge({ children, variant='neutral' }) {
  const styles = {
    success: { background:'rgba(16,185,129,.12)', color:'var(--status-success)' },
    error:   { background:'rgba(239,68,68,.12)',  color:'var(--status-error)' },
    warning: { background:'rgba(245,158,11,.12)', color:'var(--status-warning)' },
    info:    { background:'rgba(99,102,241,.12)', color:'var(--status-info)' },
    neutral: { background:'var(--surface-raised)', color:'var(--text-secondary)' },
  }[variant] || {};
  return <span style={{ display:'inline-flex', alignItems:'center', padding:'2px 8px',
    borderRadius:9999, fontSize:'11px', fontWeight:600, letterSpacing:'0.04em', textTransform:'uppercase',
    ...styles }}>{children}</span>;
}

// ── Card ─────────────────────────────────────────────────────────────────────
function Card({ children, padding='md', elevated=false, style:s={} }) {
  const p = { none:'0', sm:'8px', md:'16px', lg:'24px' }[padding];
  return <div style={{ background:'var(--surface-card)', borderRadius:'8px',
    border:'1px solid var(--border-default)', padding:p,
    boxShadow: elevated ? 'var(--shadow-md)' : 'var(--shadow-sm)', ...s }}>{children}</div>;
}

// ── Alert ────────────────────────────────────────────────────────────────────
function Alert({ children, variant='info' }) {
  const cfg = {
    info:    { icon:'Info',          color:'var(--status-info)',    bg:'rgba(99,102,241,.08)' },
    success: { icon:'CheckCircle2',  color:'var(--status-success)', bg:'rgba(16,185,129,.08)' },
    warning: { icon:'AlertTriangle', color:'var(--status-warning)', bg:'rgba(245,158,11,.08)' },
    error:   { icon:'AlertCircle',   color:'var(--status-error)',   bg:'rgba(239,68,68,.08)' },
  }[variant] || {};
  return (
    <div style={{ display:'flex', alignItems:'flex-start', gap:'10px', padding:'12px 14px',
      borderRadius:'8px', background:cfg.bg, border:`1px solid ${cfg.color}22` }}>
      <Icon name={cfg.icon} size="sm" style={{ color:cfg.color, marginTop:1, flexShrink:0 }} />
      <span style={{ fontSize:'13px', color:'var(--text-primary)', lineHeight:1.5 }}>{children}</span>
    </div>
  );
}

// ── Input ────────────────────────────────────────────────────────────────────
function Input({ type='text', value, onChange, placeholder='', disabled=false, id, style:s={} }) {
  const [foc, setFoc] = useState(false);
  const [show, setShow] = useState(false);
  const isPwd = type === 'password';
  return (
    <div style={{ position:'relative', display:'flex', alignItems:'center' }}>
      <input id={id} type={isPwd && show ? 'text' : type} value={value} onChange={onChange}
        placeholder={placeholder} disabled={disabled}
        onFocus={() => setFoc(true)} onBlur={() => setFoc(false)}
        style={{ width:'100%', padding:'9px 12px', paddingRight: isPwd ? 36 : 12,
          background:'var(--surface-bg)', color:'var(--text-primary)',
          border:`1.5px solid ${foc ? 'var(--border-focus)' : 'var(--border-default)'}`,
          borderRadius:'6px', fontSize:'14px', outline:'none',
          transition:'border-color 120ms', ...s }} />
      {isPwd && <button type="button" onClick={() => setShow(x=>!x)}
        style={{ position:'absolute', right:10, background:'none', border:'none', padding:0,
          cursor:'pointer', color:'var(--text-secondary)', display:'flex' }}>
        <Icon name={show ? 'EyeOff' : 'Eye'} size="sm" />
      </button>}
    </div>
  );
}

// ── FormField ────────────────────────────────────────────────────────────────
function FormField({ label, children, error, hint, id }) {
  return (
    <div style={{ display:'flex', flexDirection:'column', gap:'5px' }}>
      {label && <label htmlFor={id} style={{ fontSize:'11px', fontWeight:600, letterSpacing:'0.06em',
        textTransform:'uppercase', color:'var(--text-secondary)' }}>{label}</label>}
      {children}
      {error && <span style={{ fontSize:'12px', color:'var(--status-error)' }}>{error}</span>}
      {hint && !error && <span style={{ fontSize:'12px', color:'var(--text-disabled)' }}>{hint}</span>}
    </div>
  );
}

// ── Skeleton ─────────────────────────────────────────────────────────────────
function Skeleton({ width='100%', height=16, radius=6, style:s={} }) {
  return <div style={{ width, height, borderRadius:radius, background:'var(--surface-raised)',
    animation:'pulse 1.5s ease-in-out infinite', ...s }} />;
}

// ── StatCard ─────────────────────────────────────────────────────────────────
function StatCard({ label, value, icon, delta, deltaLabel, loading=false }) {
  const deltaColor = delta == null ? 'var(--text-secondary)'
    : delta >= 0 ? 'var(--status-success)' : 'var(--status-error)';
  return (
    <Card style={{ display:'flex', flexDirection:'column', gap:'10px' }}>
      <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between' }}>
        <span style={{ fontSize:'11px', fontWeight:600, letterSpacing:'0.06em',
          textTransform:'uppercase', color:'var(--text-secondary)' }}>{label}</span>
        {icon && <Icon name={icon} size="sm" style={{ color:'var(--text-secondary)' }} />}
      </div>
      {loading ? <><Skeleton height={28} width="70%" /><Skeleton height={14} width="45%" /></>
        : <>
          <span style={{ fontFamily:'JetBrains Mono, ui-monospace, monospace', fontSize:'22px',
            fontWeight:600, color:'var(--text-primary)', letterSpacing:'-0.01em' }}>{value}</span>
          {delta != null && (
            <div style={{ display:'flex', alignItems:'center', gap:5, color:deltaColor }}>
              <Icon name={delta >= 0 ? 'TrendingUp' : 'TrendingDown'} size="xs" />
              <span style={{ fontSize:'12px', fontWeight:500 }}>{deltaLabel}</span>
            </div>
          )}
        </>}
    </Card>
  );
}

// ── InstitutionAvatar ─────────────────────────────────────────────────────────
const AVATAR_COLORS = ['#4f46e5','#7c3aed','#0891b2','#059669','#d97706','#dc2626','#be185d'];
function InstitutionAvatar({ name, size=36 }) {
  const initials = name.split(/\s+/).slice(0,2).map(w=>w[0]).join('').toUpperCase();
  const color = AVATAR_COLORS[name.charCodeAt(0) % AVATAR_COLORS.length];
  return <div style={{ width:size, height:size, borderRadius:'8px', background:color+'20',
    border:`1.5px solid ${color}40`, display:'flex', alignItems:'center', justifyContent:'center',
    fontSize: size < 32 ? '10px' : '12px', fontWeight:700, color, flexShrink:0 }}>{initials}</div>;
}

// ── StatusIndicator ───────────────────────────────────────────────────────────
function StatusIndicator({ status, timestamp }) {
  const cfg = {
    synced:  { color:'var(--status-success)', label:'Synced',  dot:true },
    pending: { color:'var(--status-warning)', label:'Syncing', dot:true },
    error:   { color:'var(--status-error)',   label:'Error',   dot:true },
  }[status] || { color:'var(--text-disabled)', label:status };
  return (
    <div style={{ display:'flex', flexDirection:'column', gap:2 }}>
      <div style={{ display:'flex', alignItems:'center', gap:6 }}>
        <div style={{ width:7, height:7, borderRadius:'50%', background:cfg.color,
          boxShadow: status==='pending' ? `0 0 0 2px ${cfg.color}30` : undefined }} />
        <span style={{ fontSize:'12px', fontWeight:500, color:cfg.color }}>{cfg.label}</span>
      </div>
      {timestamp && <span style={{ fontSize:'11px', color:'var(--text-disabled)', paddingLeft:13 }}>{timestamp}</span>}
    </div>
  );
}

// ── LineChart ─────────────────────────────────────────────────────────────────
function LineChart({ data, label='', currency='USD' }) {
  const [hover, setHover] = useState(null);
  const W=560, H=160, PL=48, PR=16, PT=16, PB=32;
  const iW=W-PL-PR, iH=H-PT-PB;
  const maxVal = Math.max(...data.flatMap(d=>[d.inflow,d.outflow])) * 1.1;
  const xStep = iW/(data.length-1);
  const yScale = v => PT + iH - (v/maxVal)*iH;
  const pts = (key) => data.map((d,i)=>({ x:PL+i*xStep, y:yScale(d[key]) }));
  const line = ps => ps.map((p,i)=>`${i?'L':'M'}${p.x},${p.y}`).join(' ');
  const area = (ps, key) => {
    const l = line(ps);
    return `${l}L${PL+((ps.length-1)*xStep)},${PT+iH}L${PL},${PT+iH}Z`;
  };
  const inPts=pts('inflow'), outPts=pts('outflow'), netPts=pts('net');
  return (
    <Card style={{ padding:0 }}>
      <div style={{ padding:'16px 20px 8px', display:'flex', alignItems:'center', justifyContent:'space-between' }}>
        <span style={{ fontSize:'12px', fontWeight:600, letterSpacing:'0.05em', textTransform:'uppercase', color:'var(--text-secondary)' }}>{label}</span>
        <div style={{ display:'flex', gap:16 }}>
          {[['Inflow','#10b981'],['Outflow','#ef4444'],['Net','var(--accent-default)']].map(([n,c])=>(
            <div key={n} style={{ display:'flex', alignItems:'center', gap:5 }}>
              <div style={{ width:10, height:2.5, borderRadius:2, background:c }} />
              <span style={{ fontSize:'11px', color:'var(--text-secondary)' }}>{n}</span>
            </div>
          ))}
        </div>
      </div>
      <svg viewBox={`0 0 ${W} ${H}`} style={{ width:'100%', display:'block' }}
        onMouseLeave={() => setHover(null)}>
        {/* Grid */}
        {[0,0.25,0.5,0.75,1].map(f=>{
          const y=PT+iH*(1-f);
          return <g key={f}>
            <line x1={PL} y1={y} x2={W-PR} y2={y} stroke="var(--border-default)" strokeWidth="0.5" strokeDasharray="3 3" />
            <text x={PL-6} y={y+4} textAnchor="end" fill="var(--text-disabled)" fontSize="9">${Math.round(maxVal*f/1000)}k</text>
          </g>;
        })}
        {/* Areas */}
        <path d={area(inPts)} fill="#10b981" fillOpacity="0.07" />
        <path d={area(outPts)} fill="#ef4444" fillOpacity="0.07" />
        {/* Lines */}
        <path d={line(inPts)} fill="none" stroke="#10b981" strokeWidth="1.5" strokeLinejoin="round" />
        <path d={line(outPts)} fill="none" stroke="#ef4444" strokeWidth="1.5" strokeLinejoin="round" />
        <path d={line(netPts)} fill="none" stroke="var(--accent-default)" strokeWidth="2" strokeLinejoin="round" />
        {/* Month labels */}
        {data.map((d,i)=>(
          <text key={i} x={PL+i*xStep} y={H-6} textAnchor="middle" fill="var(--text-disabled)" fontSize="10">{d.month}</text>
        ))}
        {/* Hover targets */}
        {data.map((d,i)=>(
          <rect key={i} x={PL+i*xStep-xStep/2} y={PT} width={xStep} height={iH}
            fill="transparent" onMouseEnter={()=>setHover(i)} style={{cursor:'default'}} />
        ))}
        {/* Hover dots & tooltip */}
        {hover!=null && [inPts,outPts,netPts].map((ps,pi)=>(
          <circle key={pi} cx={ps[hover].x} cy={ps[hover].y} r="4"
            fill={['#10b981','#ef4444','var(--accent-default)'][pi]}
            stroke="var(--surface-card)" strokeWidth="2" />
        ))}
        {hover!=null && (() => {
          const d=data[hover]; const x=PL+hover*xStep; const tx=x>W-120?x-110:x+12;
          return <g>
            <line x1={x} y1={PT} x2={x} y2={PT+iH} stroke="var(--border-default)" strokeWidth="1" strokeDasharray="3 3" />
            <rect x={tx-4} y={PT} width={106} height={66} rx="5" fill="var(--surface-card)" stroke="var(--border-default)" strokeWidth="1" />
            <text x={tx+2} y={PT+14} fill="var(--text-secondary)" fontSize="10" fontWeight="600">{d.month}</text>
            <text x={tx+2} y={PT+28} fill="#10b981" fontSize="10">↑ {fmt(d.inflow)}</text>
            <text x={tx+2} y={PT+42} fill="#ef4444" fontSize="10">↓ {fmt(d.outflow)}</text>
            <text x={tx+2} y={PT+58} fill="var(--accent-default)" fontSize="10" fontWeight="600">Net {fmt(d.net)}</text>
          </g>;
        })()}
      </svg>
    </Card>
  );
}

// ── DonutChart ────────────────────────────────────────────────────────────────
function DonutChart({ segments, label='' }) {
  const [hov, setHov] = useState(null);
  const total = segments.reduce((s,d)=>s+d.value,0);
  let angle = -90;
  const arcs = segments.map((seg,i) => {
    const sweep = (seg.value/total)*360;
    const a1=angle, a2=angle+sweep;
    angle+=sweep;
    const p2c = (deg,r) => ({ x:100+r*Math.cos(deg*Math.PI/180), y:100+r*Math.sin(deg*Math.PI/180) });
    const s1=p2c(a1,72), e1=p2c(a2,72), s2=p2c(a2,48), e2=p2c(a1,48);
    const large=sweep>180?1:0;
    return { ...seg, d:`M${s1.x},${s1.y} A72,72 0 ${large},1 ${e1.x},${e1.y} L${s2.x},${s2.y} A48,48 0 ${large},0 ${e2.x},${e2.y}Z`, i };
  });
  const hovSeg = hov!=null ? segments[hov] : null;
  return (
    <Card style={{ padding:0 }}>
      <div style={{ padding:'16px 20px 0' }}>
        <span style={{ fontSize:'12px', fontWeight:600, letterSpacing:'0.05em', textTransform:'uppercase', color:'var(--text-secondary)' }}>{label}</span>
      </div>
      <div style={{ display:'flex', alignItems:'center', gap:16, padding:'12px 20px 16px' }}>
        <svg viewBox="0 0 200 200" style={{ width:130, flexShrink:0 }}>
          {arcs.map((arc,i)=>(
            <path key={i} d={arc.d} fill={arc.color}
              opacity={hov==null||hov===i?1:0.4} style={{cursor:'pointer',transition:'opacity 120ms'}}
              onMouseEnter={()=>setHov(i)} onMouseLeave={()=>setHov(null)} />
          ))}
          <text x="100" y="96" textAnchor="middle" fill="var(--text-primary)" fontSize="13" fontWeight="700">
            {hovSeg ? `${hovSeg.pct}%` : `${fmt(total)}`}
          </text>
          <text x="100" y="112" textAnchor="middle" fill="var(--text-secondary)" fontSize="9">
            {hovSeg ? hovSeg.name : 'total spend'}
          </text>
        </svg>
        <div style={{ display:'flex', flexDirection:'column', gap:6, flex:1 }}>
          {segments.map((s,i)=>(
            <div key={i} style={{ display:'flex', alignItems:'center', justifyContent:'space-between', gap:8,
              opacity:hov==null||hov===i?1:0.4, transition:'opacity 120ms', cursor:'pointer' }}
              onMouseEnter={()=>setHov(i)} onMouseLeave={()=>setHov(null)}>
              <div style={{ display:'flex', alignItems:'center', gap:7 }}>
                <div style={{ width:9, height:9, borderRadius:'50%', background:s.color, flexShrink:0 }} />
                <span style={{ fontSize:'12px', color:'var(--text-secondary)' }}>{s.name}</span>
              </div>
              <span style={{ fontSize:'12px', fontWeight:600, fontFamily:'JetBrains Mono,monospace', color:'var(--text-primary)' }}>{s.pct}%</span>
            </div>
          ))}
        </div>
      </div>
    </Card>
  );
}

// ── DataTable ─────────────────────────────────────────────────────────────────
function DataTable({ columns, rows, emptyMessage='No data', loading=false, onRowClick }) {
  return (
    <Card padding="none">
      <div style={{ overflowX:'auto' }}>
        <table style={{ width:'100%', borderCollapse:'collapse', fontSize:'13px' }}>
          <thead>
            <tr style={{ borderBottom:'1px solid var(--border-default)' }}>
              {columns.map(col=>(
                <th key={col.key} style={{ padding:'10px 16px', fontWeight:600, fontSize:'11px',
                  letterSpacing:'0.06em', textTransform:'uppercase', color:'var(--text-secondary)',
                  textAlign:col.align||'left', whiteSpace:'nowrap' }}>{col.header}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading ? Array(4).fill(0).map((_,i)=>(
              <tr key={i}><td colSpan={columns.length} style={{ padding:'12px 16px' }}>
                <Skeleton height={14} width={`${60+Math.random()*30}%`} />
              </td></tr>
            )) : rows.length === 0 ? (
              <tr><td colSpan={columns.length} style={{ padding:'32px', textAlign:'center', color:'var(--text-disabled)', fontSize:'13px' }}>{emptyMessage}</td></tr>
            ) : rows.map((row, ri)=>(
              <tr key={ri} style={{ borderBottom: ri<rows.length-1?'1px solid var(--border-default)':undefined,
                transition:'background 100ms', cursor: onRowClick ? 'pointer' : 'default' }}
                onClick={() => onRowClick && onRowClick(row)}
                onMouseEnter={e=>e.currentTarget.style.background='var(--surface-bg)'}
                onMouseLeave={e=>e.currentTarget.style.background=''}>
                {columns.map(col=>(
                  <td key={col.key} style={{ padding:'12px 16px', color:'var(--text-primary)',
                    textAlign:col.align||'left', ...(col.mono ? {fontFamily:'JetBrains Mono,monospace'} : {}) }}>
                    {col.cell ? col.cell(row) : row[col.key]}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Card>
  );
}

// ── NetWorthChart ─────────────────────────────────────────────────────────────
function NetWorthChart({ data }) {
  const [hover, setHover] = useState(null);
  const [range, setRange] = useState('1Y');

  const sliced = range === '3M' ? data.slice(-3) : range === '6M' ? data.slice(-6) : data;

  const W=700, H=200, PL=64, PR=20, PT=20, PB=32;
  const iW=W-PL-PR, iH=H-PT-PB;

  const maxVal = Math.max(...sliced.map(d=>d.total)) * 1.08;
  const minVal = Math.min(...sliced.map(d=>d.total)) * 0.93;
  const range_ = maxVal - minVal;

  const xStep = sliced.length > 1 ? iW / (sliced.length - 1) : iW;
  const yScale = v => PT + iH - ((v - minVal) / range_) * iH;

  const stackedArea = (key, prevKey) => {
    const pts = sliced.map((d, i) => {
      const base = prevKey ? d[prevKey] : 0;
      return { x: PL + i * xStep, yTop: yScale(base + d[key]), yBot: yScale(base) };
    });
    const top = pts.map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x},${p.yTop}`).join(' ');
    const bot = [...pts].reverse().map((p, i) => `${i === 0 ? 'L' : 'L'}${p.x},${p.yBot}`).join(' ');
    return `${top} ${bot} Z`;
  };

  const totalLine = sliced.map((d, i) => `${i === 0 ? 'M' : 'L'}${PL + i * xStep},${yScale(d.total)}`).join(' ');

  const LAYERS = [
    { key:'banking',   prev:null,        color:'#4f46e5', label:'Banking' },
    { key:'brokerage', prev:'banking',   color:'#10b981', label:'Brokerage' },
    { key:'crypto',    prev:'brokerage', color:'#f59e0b', label:'Crypto' },
  ];

  const startVal = sliced[0]?.total ?? 0;
  const endVal   = sliced[sliced.length - 1]?.total ?? 0;
  const change   = endVal - startVal;
  const changePct = startVal > 0 ? ((change / startVal) * 100).toFixed(1) : '0.0';
  const positive  = change >= 0;

  return (
    <Card style={{ padding:0 }}>
      {/* Header */}
      <div style={{ display:'flex', alignItems:'flex-start', justifyContent:'space-between',
        padding:'18px 20px 0' }}>
        <div>
          <span style={{ fontSize:'11px', fontWeight:600, letterSpacing:'0.06em',
            textTransform:'uppercase', color:'var(--text-secondary)' }}>Net Worth Over Time</span>
          <div style={{ display:'flex', alignItems:'baseline', gap:10, marginTop:6 }}>
            <span style={{ fontFamily:'JetBrains Mono,monospace', fontSize:'26px', fontWeight:700,
              color:'var(--text-primary)', letterSpacing:'-0.02em' }}>{fmt(endVal)}</span>
            <span style={{ fontSize:'13px', fontWeight:500,
              color: positive ? 'var(--status-success)' : 'var(--status-error)' }}>
              {positive ? '+' : ''}{fmt(change)} ({positive ? '+' : ''}{changePct}%)
            </span>
          </div>
        </div>
        {/* Range pills */}
        <div style={{ display:'flex', gap:4 }}>
          {['3M','6M','1Y'].map(r => (
            <button key={r} onClick={() => setRange(r)}
              style={{ padding:'4px 10px', borderRadius:6, fontSize:'12px', fontWeight:500,
                border:'1.5px solid', cursor:'pointer', fontFamily:'inherit', transition:'all 120ms',
                borderColor: range===r ? 'var(--accent-default)' : 'var(--border-default)',
                background:  range===r ? 'var(--accent-subtle)'  : 'transparent',
                color:       range===r ? 'var(--accent-default)' : 'var(--text-secondary)' }}>
              {r}
            </button>
          ))}
        </div>
      </div>

      {/* Legend */}
      <div style={{ display:'flex', gap:18, padding:'10px 20px 0' }}>
        {LAYERS.map(l => (
          <div key={l.key} style={{ display:'flex', alignItems:'center', gap:6 }}>
            <div style={{ width:10, height:10, borderRadius:2, background:l.color }} />
            <span style={{ fontSize:'11px', color:'var(--text-secondary)' }}>{l.label}</span>
          </div>
        ))}
        <div style={{ display:'flex', alignItems:'center', gap:6 }}>
          <div style={{ width:18, height:2, background:'var(--text-primary)', borderRadius:1 }} />
          <span style={{ fontSize:'11px', color:'var(--text-secondary)' }}>Total</span>
        </div>
      </div>

      {/* SVG */}
      <svg viewBox={`0 0 ${W} ${H}`} style={{ width:'100%', display:'block', marginTop:4 }}
        onMouseLeave={() => setHover(null)}>
        {/* Y grid */}
        {[0, 0.33, 0.66, 1].map(f => {
          const val = minVal + range_ * f;
          const y = yScale(val);
          return (
            <g key={f}>
              <line x1={PL} y1={y} x2={W-PR} y2={y} stroke="var(--border-default)"
                strokeWidth="0.5" strokeDasharray="3 3" />
              <text x={PL-8} y={y+4} textAnchor="end" fill="var(--text-disabled)" fontSize="9">
                ${Math.round(val/1000)}k
              </text>
            </g>
          );
        })}

        {/* Stacked areas */}
        {LAYERS.map(l => (
          <path key={l.key} d={stackedArea(l.key, l.prev)}
            fill={l.color} fillOpacity="0.18" />
        ))}

        {/* Total line */}
        <path d={totalLine} fill="none" stroke="var(--text-primary)" strokeWidth="2"
          strokeLinejoin="round" />

        {/* X labels */}
        {sliced.map((d, i) => (
          <text key={i} x={PL + i * xStep} y={H - 6} textAnchor="middle"
            fill="var(--text-disabled)" fontSize="9">{d.month}</text>
        ))}

        {/* Hover targets */}
        {sliced.map((_, i) => (
          <rect key={i} x={PL + i * xStep - xStep / 2} y={PT} width={xStep} height={iH}
            fill="transparent" onMouseEnter={() => setHover(i)} style={{ cursor:'default' }} />
        ))}

        {/* Hover dot + tooltip */}
        {hover != null && (() => {
          const d = sliced[hover];
          const x = PL + hover * xStep;
          const y = yScale(d.total);
          const tx = x > W - 150 ? x - 148 : x + 12;
          return (
            <g>
              <line x1={x} y1={PT} x2={x} y2={PT+iH} stroke="var(--border-default)"
                strokeWidth="1" strokeDasharray="3 3" />
              <circle cx={x} cy={y} r="5" fill="var(--text-primary)"
                stroke="var(--surface-card)" strokeWidth="2" />
              <rect x={tx-4} y={PT} width={148} height={82} rx="6"
                fill="var(--surface-card)" stroke="var(--border-default)" strokeWidth="1" />
              <text x={tx+4} y={PT+14} fill="var(--text-secondary)" fontSize="10" fontWeight="600">{d.month}</text>
              {LAYERS.map((l, li) => (
                <g key={l.key}>
                  <rect x={tx+4} y={PT+20+li*16+3} width={8} height={8} rx="2" fill={l.color} />
                  <text x={tx+16} y={PT+20+li*16+11} fill="var(--text-secondary)" fontSize="9">{l.label}</text>
                  <text x={tx+140} y={PT+20+li*16+11} textAnchor="end" fill="var(--text-primary)" fontSize="9" fontWeight="600">${Math.round(d[l.key]/1000)}k</text>
                </g>
              ))}
              <text x={tx+4} y={PT+78} fill="var(--text-primary)" fontSize="10" fontWeight="700">Total {fmt(d.total)}</text>
            </g>
          );
        })()}
      </svg>
    </Card>
  );
}

Object.assign(window, { Icon, Button, Badge, Card, Alert, Input, FormField, Skeleton,
  StatCard, InstitutionAvatar, StatusIndicator, LineChart, DonutChart, DataTable, NetWorthChart });
