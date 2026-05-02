// Finance Sentry — Layout Components (Sidebar, TopBar, AppLayout, Toast, Modal)
const { useState, useEffect, useRef, useContext, createContext, useCallback, useMemo } = React;

// ── Toast System ──────────────────────────────────────────────────────────────
const ToastContext = createContext(null);

function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([]);
  const addToast = useCallback((message, variant='info', duration=4000) => {
    const id = Date.now() + Math.random();
    setToasts(ts => [...ts, { id, message, variant }]);
    setTimeout(() => setToasts(ts => ts.filter(t => t.id !== id)), duration);
  }, []);
  const remove = id => setToasts(ts => ts.filter(t => t.id !== id));

  const iconMap = { success:'CheckCircle2', error:'AlertCircle', warning:'AlertTriangle', info:'Info' };
  const colorMap = { success:'var(--status-success)', error:'var(--status-error)',
    warning:'var(--status-warning)', info:'var(--status-info)' };

  return (
    <ToastContext.Provider value={{ addToast }}>
      {children}
      <div style={{ position:'fixed', bottom:24, right:24, display:'flex', flexDirection:'column',
        gap:8, zIndex:9999, pointerEvents:'none' }}>
        {toasts.map(t => (
          <div key={t.id} style={{ display:'flex', alignItems:'flex-start', gap:10, padding:'12px 14px',
            background:'var(--surface-card)', border:'1px solid var(--border-default)',
            borderRadius:10, boxShadow:'var(--shadow-md)', minWidth:280, maxWidth:380,
            animation:'slideInRight 0.22s ease', pointerEvents:'auto' }}>
            <Icon name={iconMap[t.variant]||'Info'} size="sm"
              style={{ color:colorMap[t.variant], flexShrink:0, marginTop:1 }} />
            <span style={{ fontSize:'13px', color:'var(--text-primary)', lineHeight:1.45, flex:1 }}>{t.message}</span>
            <button onClick={() => remove(t.id)} style={{ background:'none', border:'none', padding:0,
              cursor:'pointer', color:'var(--text-disabled)', display:'flex', flexShrink:0 }}>
              <Icon name="X" size="xs" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

function useToast() { return useContext(ToastContext); }

// ── Modal ─────────────────────────────────────────────────────────────────────
function Modal({ open, onClose, title, children, size='md', footer }) {
  useEffect(() => {
    if (open) document.body.style.overflow = 'hidden';
    else document.body.style.overflow = '';
    return () => { document.body.style.overflow = ''; };
  }, [open]);
  if (!open) return null;
  const widths = { sm:420, md:540, lg:680 };
  return (
    <div style={{ position:'fixed', inset:0, zIndex:1000, display:'flex',
      alignItems:'center', justifyContent:'center', padding:16 }}
      onClick={e => e.target === e.currentTarget && onClose()}>
      <div style={{ position:'absolute', inset:0, background:'rgba(0,0,0,0.55)',
        backdropFilter:'blur(3px)', animation:'fadeIn 0.15s ease' }} onClick={onClose} />
      <div style={{ position:'relative', width:'100%', maxWidth:widths[size],
        background:'var(--surface-card)', borderRadius:12, border:'1px solid var(--border-default)',
        boxShadow:'var(--shadow-md)', animation:'slideIn 0.2s ease', overflow:'hidden', zIndex:1 }}>
        {/* Header */}
        <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between',
          padding:'18px 22px', borderBottom:'1px solid var(--border-default)' }}>
          <h2 style={{ fontSize:'16px', fontWeight:600, color:'var(--text-primary)' }}>{title}</h2>
          <button onClick={onClose} style={{ background:'none', border:'none', padding:4, cursor:'pointer',
            color:'var(--text-secondary)', borderRadius:6, display:'flex',
            transition:'background 120ms' }}
            onMouseEnter={e=>e.currentTarget.style.background='var(--surface-raised)'}
            onMouseLeave={e=>e.currentTarget.style.background='none'}>
            <Icon name="X" size="sm" />
          </button>
        </div>
        {/* Body */}
        <div style={{ padding:'22px', maxHeight:'70vh', overflowY:'auto' }}>{children}</div>
        {/* Footer */}
        {footer && <div style={{ padding:'16px 22px', borderTop:'1px solid var(--border-default)',
          display:'flex', justifyContent:'flex-end', gap:10 }}>{footer}</div>}
      </div>
    </div>
  );
}

// ── ConfirmDialog ─────────────────────────────────────────────────────────────
function ConfirmDialog({ open, onClose, onConfirm, title, message, confirmLabel='Confirm',
    confirmVariant='primary', loading=false }) {
  return (
    <Modal open={open} onClose={onClose} title={title} size="sm"
      footer={<>
        <Button variant="secondary" onClick={onClose} disabled={loading}>Cancel</Button>
        <Button variant={confirmVariant} onClick={onConfirm} loading={loading}>{confirmLabel}</Button>
      </>}>
      <p style={{ fontSize:'14px', color:'var(--text-secondary)', lineHeight:1.6 }}>{message}</p>
    </Modal>
  );
}

// ── Sidebar ───────────────────────────────────────────────────────────────────
function NavBtn({ item, activePage, collapsed, onNavigate }) {
  const active = activePage === item.id;
  return (
    <button onClick={() => onNavigate(item.id)} title={collapsed ? item.label : ''}
      style={{ display:'flex', alignItems:'center', gap:10,
        padding: collapsed ? '9px 0' : '9px 12px',
        justifyContent: collapsed ? 'center' : 'flex-start',
        borderRadius:6, border:'none', cursor:'pointer', width:'100%', textAlign:'left',
        background: active ? 'var(--accent-subtle)' : 'transparent',
        color: active ? 'var(--accent-default)' : 'var(--text-secondary)',
        transition:'background 120ms, color 120ms', fontWeight: active ? 600 : 400,
        fontFamily:'inherit', fontSize:'14px' }}
      onMouseEnter={e=>{ if(!active){e.currentTarget.style.background='var(--surface-raised)';e.currentTarget.style.color='var(--text-primary)'}}}
      onMouseLeave={e=>{ if(!active){e.currentTarget.style.background='transparent';e.currentTarget.style.color='var(--text-secondary)'}}}>
      <Icon name={item.icon} size="sm" style={{ flexShrink:0 }} />
      {!collapsed && <span style={{ whiteSpace:'nowrap', overflow:'hidden', textOverflow:'ellipsis' }}>{item.label}</span>}
    </button>
  );
}

const NAV_ITEMS = [
  { id:'dashboard',     label:'Dashboard',       icon:'LayoutDashboard' },
  { id:'accounts',      label:'Accounts',        icon:'CreditCard' },
  { id:'transactions',  label:'Transactions',    icon:'ArrowLeftRight' },
  { id:'holdings',      label:'Holdings',        icon:'PieChart' },
  { id:'budgets',       label:'Budgets',         icon:'Zap' },
  { id:'subscriptions', label:'Subscriptions',   icon:'RefreshCw' },
  { id:'settings',      label:'Settings',        icon:'ShieldCheck', bottom:true },
];

function Sidebar({ activePage, onNavigate }) {
  const [collapsed, setCollapsed] = useState(false);
  const W = collapsed ? 64 : 236;
  return (
    <aside style={{ width:W, flexShrink:0, height:'100%', display:'flex', flexDirection:'column',
      background:'var(--surface-card)', borderRight:'1px solid var(--border-default)',
      transition:'width 200ms cubic-bezier(0.4,0,0.2,1)', overflow:'hidden' }}>
      {/* Logo row */}
      <div style={{ height:56, display:'flex', alignItems:'center', justifyContent:'space-between',
        padding:'0 14px', borderBottom:'1px solid var(--border-default)', flexShrink:0 }}>
        {!collapsed && (
          <div style={{ display:'flex', alignItems:'center', gap:9, overflow:'hidden' }}>
            <div style={{ width:28, height:28, borderRadius:8, background:'var(--accent-default)',
              display:'flex', alignItems:'center', justifyContent:'center', flexShrink:0 }}>
              <Icon name="ShieldCheck" size="xs" style={{ color:'white' }} />
            </div>
            <span style={{ fontSize:'14px', fontWeight:700, color:'var(--text-primary)',
              whiteSpace:'nowrap', letterSpacing:'-0.01em' }}>Finance Sentry</span>
          </div>
        )}
        {collapsed && <div style={{ width:28, height:28, borderRadius:8, background:'var(--accent-default)',
          display:'flex', alignItems:'center', justifyContent:'center', margin:'0 auto' }}>
          <Icon name="ShieldCheck" size="xs" style={{ color:'white' }} />
        </div>}
        {!collapsed && <button onClick={() => setCollapsed(true)}
          style={{ background:'none', border:'none', padding:5, cursor:'pointer', borderRadius:6,
            color:'var(--text-secondary)', display:'flex', flexShrink:0,
            transition:'background 120ms, color 120ms' }}
          onMouseEnter={e=>{e.currentTarget.style.background='var(--surface-raised)';e.currentTarget.style.color='var(--text-primary)'}}
          onMouseLeave={e=>{e.currentTarget.style.background='none';e.currentTarget.style.color='var(--text-secondary)'}}>
          <Icon name="PanelLeftClose" size="sm" />
        </button>}
        {collapsed && <button onClick={() => setCollapsed(false)}
          style={{ display:'none' }} />}
      </div>
      {/* Nav items */}
      <nav style={{ flex:1, padding:8, display:'flex', flexDirection:'column', gap:2 }}>
        {NAV_ITEMS.filter(i => !i.bottom).map(item => <NavBtn key={item.id} item={item} activePage={activePage} collapsed={collapsed} onNavigate={onNavigate} />)}
      </nav>
      {/* Bottom nav items (Settings) */}
      <div style={{ padding:8, borderTop:'1px solid var(--border-default)', display:'flex', flexDirection:'column', gap:2 }}>
        {NAV_ITEMS.filter(i => i.bottom).map(item => <NavBtn key={item.id} item={item} activePage={activePage} collapsed={collapsed} onNavigate={onNavigate} />)}
      </div>
      {/* Expand button when collapsed */}
      {collapsed && (
        <button onClick={() => setCollapsed(false)}
          style={{ margin:'0 auto 12px', background:'none', border:'none', padding:8, cursor:'pointer',
            borderRadius:6, color:'var(--text-secondary)', display:'flex',
            transition:'background 120ms' }}
          onMouseEnter={e=>e.currentTarget.style.background='var(--surface-raised)'}
          onMouseLeave={e=>e.currentTarget.style.background='none'}
          title="Expand sidebar">
          <Icon name="PanelLeftOpen" size="sm" />
        </button>
      )}
    </aside>
  );
}

// ── TopBar ────────────────────────────────────────────────────────────────────
const PAGE_TITLES = { dashboard:'Dashboard', accounts:'Account Inventory',
  transactions:'Transactions', holdings:'Asset Allocation',
  budgets:'Budgets', subscriptions:'Subscriptions', settings:'Settings' };

// ── Command Palette ───────────────────────────────────────────────────────────
const PALETTE_ITEMS = [
  { id:'dashboard',     label:'Dashboard',             icon:'LayoutDashboard', group:'Pages' },
  { id:'accounts',      label:'Account Inventory',     icon:'CreditCard',      group:'Pages' },
  { id:'transactions',  label:'Transactions',          icon:'ArrowLeftRight',  group:'Pages' },
  { id:'holdings',      label:'Asset Allocation',      icon:'PieChart',        group:'Pages' },
  { id:'budgets',       label:'Budgets',               icon:'Zap',             group:'Pages' },
  { id:'subscriptions', label:'Subscriptions',         icon:'RefreshCw',       group:'Pages' },
  { id:'settings',      label:'Settings',              icon:'ShieldCheck',     group:'Pages' },
  { id:'_connect',      label:'Connect Account',       icon:'Link',            group:'Actions' },
  { id:'_theme',        label:'Toggle Dark Mode',      icon:'Moon',            group:'Actions' },
  { id:'_logout',       label:'Sign Out',              icon:'LogOut',          group:'Actions' },
];

function CommandPalette({ open, onClose, onNavigate, onAction }) {
  const [query, setQuery] = useState('');
  const [sel, setSel] = useState(0);
  const inputRef = useRef(null);

  useEffect(() => {
    if (open) { setQuery(''); setSel(0); setTimeout(() => inputRef.current?.focus(), 30); }
  }, [open]);

  const filtered = useMemo(() => {
    const q = query.toLowerCase();
    return PALETTE_ITEMS.filter(i => i.label.toLowerCase().includes(q) || i.group.toLowerCase().includes(q));
  }, [query]);

  useEffect(() => { setSel(0); }, [filtered.length]);

  useEffect(() => {
    if (!open) return;
    const handler = e => {
      if (e.key === 'ArrowDown') { e.preventDefault(); setSel(s => Math.min(s+1, filtered.length-1)); }
      if (e.key === 'ArrowUp')   { e.preventDefault(); setSel(s => Math.max(s-1, 0)); }
      if (e.key === 'Enter')     { e.preventDefault(); activate(filtered[sel]); }
      if (e.key === 'Escape')    { onClose(); }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [open, filtered, sel]);

  const activate = item => {
    if (!item) return;
    onClose();
    if (item.id.startsWith('_')) onAction(item.id);
    else onNavigate(item.id);
  };

  // Group items
  const groups = filtered.reduce((acc, item) => {
    if (!acc[item.group]) acc[item.group] = [];
    acc[item.group].push(item);
    return acc;
  }, {});

  let globalIdx = 0;

  if (!open) return null;

  return (
    <div style={{ position:'fixed', inset:0, zIndex:2000, display:'flex',
      alignItems:'flex-start', justifyContent:'center', paddingTop:'18vh' }}
      onClick={e => e.target === e.currentTarget && onClose()}>
      <div style={{ position:'absolute', inset:0, background:'rgba(0,0,0,0.45)',
        backdropFilter:'blur(4px)', animation:'fadeIn 0.12s ease' }} onClick={onClose} />
      <div style={{ position:'relative', width:'100%', maxWidth:560, zIndex:1,
        background:'var(--surface-card)', borderRadius:14, border:'1px solid var(--border-default)',
        boxShadow:'var(--shadow-md)', overflow:'hidden', animation:'slideIn 0.16s ease' }}>
        {/* Search input */}
        <div style={{ display:'flex', alignItems:'center', gap:12, padding:'14px 16px',
          borderBottom:'1px solid var(--border-default)' }}>
          <Icon name="Search" size="sm" style={{ color:'var(--text-secondary)', flexShrink:0 }} />
          <input ref={inputRef} value={query} onChange={e => setQuery(e.target.value)}
            placeholder="Search pages, actions…"
            style={{ flex:1, border:'none', background:'transparent', outline:'none',
              fontSize:'15px', color:'var(--text-primary)', fontFamily:'inherit' }} />
          <kbd style={{ padding:'2px 7px', borderRadius:5, border:'1px solid var(--border-default)',
            fontSize:'11px', color:'var(--text-disabled)', fontFamily:'inherit', flexShrink:0 }}>ESC</kbd>
        </div>

        {/* Results */}
        <div style={{ maxHeight:360, overflowY:'auto', padding:'6px 0' }}>
          {filtered.length === 0 ? (
            <div style={{ padding:'24px', textAlign:'center', fontSize:'13px', color:'var(--text-disabled)' }}>
              No results for "{query}"
            </div>
          ) : Object.entries(groups).map(([group, items]) => (
            <div key={group}>
              <div style={{ padding:'6px 16px 4px', fontSize:'10px', fontWeight:700,
                letterSpacing:'0.08em', textTransform:'uppercase', color:'var(--text-disabled)' }}>
                {group}
              </div>
              {items.map(item => {
                const idx = globalIdx++;
                const active = idx === sel;
                return (
                  <div key={item.id} onClick={() => activate(item)}
                    onMouseEnter={() => setSel(idx)}
                    style={{ display:'flex', alignItems:'center', gap:12, padding:'9px 16px',
                      cursor:'pointer', transition:'background 80ms',
                      background: active ? 'var(--accent-subtle)' : 'transparent' }}>
                    <div style={{ width:30, height:30, borderRadius:8,
                      background: active ? 'var(--accent-default)' : 'var(--surface-raised)',
                      display:'flex', alignItems:'center', justifyContent:'center', flexShrink:0,
                      transition:'background 80ms' }}>
                      <Icon name={item.icon} size="xs"
                        style={{ color: active ? 'white' : 'var(--text-secondary)' }} />
                    </div>
                    <span style={{ fontSize:'14px', color: active ? 'var(--accent-default)' : 'var(--text-primary)',
                      fontWeight: active ? 500 : 400 }}>{item.label}</span>
                    {active && <div style={{ marginLeft:'auto' }}>
                      <kbd style={{ padding:'2px 6px', borderRadius:4, border:'1px solid var(--border-default)',
                        fontSize:'10px', color:'var(--text-disabled)', fontFamily:'inherit' }}>↵</kbd>
                    </div>}
                  </div>
                );
              })}
            </div>
          ))}
        </div>

        {/* Footer hint */}
        <div style={{ padding:'8px 16px', borderTop:'1px solid var(--border-default)',
          display:'flex', gap:14 }}>
          {[['↑↓','Navigate'],['↵','Select'],['ESC','Close']].map(([k,l]) => (
            <div key={k} style={{ display:'flex', alignItems:'center', gap:5 }}>
              <kbd style={{ padding:'1px 5px', borderRadius:3, border:'1px solid var(--border-default)',
                fontSize:'10px', color:'var(--text-disabled)', fontFamily:'inherit' }}>{k}</kbd>
              <span style={{ fontSize:'11px', color:'var(--text-disabled)' }}>{l}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function TopBar({ activePage, dark, onToggleDark, onNavigate, onOpenPalette, onConnect }) {
  const [userOpen, setUserOpen] = useState(false);
  const ref = useRef(null);
  useEffect(() => {
    const h = e => { if (ref.current && !ref.current.contains(e.target)) setUserOpen(false); };
    document.addEventListener('mousedown', h);
    return () => document.removeEventListener('mousedown', h);
  }, []);

  return (
    <header style={{ height:56, display:'flex', alignItems:'center', gap:16, flexShrink:0,
      padding:'0 24px', background:'var(--surface-card)', borderBottom:'1px solid var(--border-default)' }}>
      <h1 style={{ fontSize:'15px', fontWeight:600, color:'var(--text-primary)' }}>
        {PAGE_TITLES[activePage] || ''}
      </h1>
      <div style={{ flex:1 }} />
      {/* Search pill — opens command palette */}
      <div onClick={onOpenPalette}
        style={{ display:'flex', alignItems:'center', gap:8, padding:'6px 12px',
          background:'var(--surface-bg)', border:'1px solid var(--border-default)',
          borderRadius:8, cursor:'pointer', color:'var(--text-secondary)', fontSize:'13px',
          transition:'border-color 120ms, color 120ms', userSelect:'none' }}
        onMouseEnter={e=>{e.currentTarget.style.borderColor='var(--border-strong)';e.currentTarget.style.color='var(--text-primary)'}}
        onMouseLeave={e=>{e.currentTarget.style.borderColor='var(--border-default)';e.currentTarget.style.color='var(--text-secondary)'}}>
        <Icon name="Search" size="xs" />
        <span>Search…</span>
        <kbd style={{ marginLeft:8, padding:'1px 6px', borderRadius:4, border:'1px solid var(--border-default)',
          fontSize:'11px', color:'var(--text-disabled)', fontFamily:'inherit' }}>⌘K</kbd>
      </div>
      {/* Theme toggle */}
      <IconBtn onClick={onToggleDark} title={dark ? 'Light mode' : 'Dark mode'}>
        <Icon name={dark ? 'Sun' : 'Moon'} size="sm" />
      </IconBtn>
      {/* Avatar + dropdown */}
      <div ref={ref} style={{ position:'relative' }}>
        <button onClick={() => setUserOpen(x=>!x)}
          style={{ width:34, height:34, borderRadius:'50%', background:'var(--accent-default)',
            border:'none', cursor:'pointer', fontSize:'12px', fontWeight:700,
            color:'var(--text-inverse)', display:'flex', alignItems:'center', justifyContent:'center',
            transition:'opacity 120ms' }}
          onMouseEnter={e=>e.currentTarget.style.opacity='0.85'}
          onMouseLeave={e=>e.currentTarget.style.opacity='1'}>JD</button>
        {userOpen && (
          <div style={{ position:'absolute', top:'calc(100% + 8px)', right:0, width:200,
            background:'var(--surface-card)', border:'1px solid var(--border-default)',
            borderRadius:10, boxShadow:'var(--shadow-md)', overflow:'hidden', zIndex:200 }}>
            <div style={{ padding:'12px 14px', borderBottom:'1px solid var(--border-default)' }}>
              <div style={{ fontSize:'13px', fontWeight:600, color:'var(--text-primary)' }}>John Doe</div>
              <div style={{ fontSize:'12px', color:'var(--text-secondary)' }}>john@example.com</div>
            </div>
            {[['ShieldCheck','Security'],['Key','API Keys']].map(([icon,label])=>(
              <button key={label} style={{ display:'flex', alignItems:'center', gap:10, padding:'10px 14px',
                width:'100%', background:'none', border:'none', cursor:'pointer', fontSize:'13px',
                color:'var(--text-secondary)', transition:'background 100ms' }}
                onMouseEnter={e=>{e.currentTarget.style.background='var(--surface-bg)';e.currentTarget.style.color='var(--text-primary)'}}
                onMouseLeave={e=>{e.currentTarget.style.background='none';e.currentTarget.style.color='var(--text-secondary)'}}>
                <Icon name={icon} size="xs" />{label}
              </button>
            ))}
            <div style={{ borderTop:'1px solid var(--border-default)' }}>
              <button onClick={()=>onNavigate('login')} style={{ display:'flex', alignItems:'center', gap:10,
                padding:'10px 14px', width:'100%', background:'none', border:'none', cursor:'pointer',
                fontSize:'13px', color:'var(--status-error)', transition:'background 100ms' }}
                onMouseEnter={e=>e.currentTarget.style.background='rgba(239,68,68,.06)'}
                onMouseLeave={e=>e.currentTarget.style.background='none'}>
                <Icon name="LogOut" size="xs" />Sign out
              </button>
            </div>
          </div>
        )}
      </div>
    </header>
  );
}

function IconBtn({ children, onClick, title }) {
  return (
    <button onClick={onClick} title={title}
      style={{ width:34, height:34, display:'flex', alignItems:'center', justifyContent:'center',
        background:'none', border:'none', borderRadius:8, cursor:'pointer',
        color:'var(--text-secondary)', transition:'background 120ms, color 120ms' }}
      onMouseEnter={e=>{e.currentTarget.style.background='var(--surface-raised)';e.currentTarget.style.color='var(--text-primary)'}}
      onMouseLeave={e=>{e.currentTarget.style.background='none';e.currentTarget.style.color='var(--text-secondary)'}}>
      {children}
    </button>
  );
}

// ── AppLayout ─────────────────────────────────────────────────────────────────
function AppLayout({ activePage, dark, onToggleDark, onNavigate, onConnect, children }) {
  const [paletteOpen, setPaletteOpen] = useState(false);

  // ⌘K / Ctrl+K global shortcut
  useEffect(() => {
    const h = e => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') { e.preventDefault(); setPaletteOpen(true); }
    };
    window.addEventListener('keydown', h);
    return () => window.removeEventListener('keydown', h);
  }, []);

  const handlePaletteAction = id => {
    if (id === '_connect')  onConnect();
    if (id === '_theme')    onToggleDark();
    if (id === '_logout')   onNavigate('login');
  };

  return (
    <div style={{ display:'flex', height:'100%', overflow:'hidden', background:'var(--surface-bg)' }}>
      <Sidebar activePage={activePage} onNavigate={onNavigate} />
      <div style={{ flex:1, display:'flex', flexDirection:'column', overflow:'hidden' }}>
        <TopBar activePage={activePage} dark={dark} onToggleDark={onToggleDark}
          onNavigate={onNavigate} onOpenPalette={() => setPaletteOpen(true)} onConnect={onConnect} />
        <main style={{ flex:1, overflowY:'auto' }}>{children}</main>
      </div>
      <CommandPalette open={paletteOpen} onClose={() => setPaletteOpen(false)}
        onNavigate={id => { setPaletteOpen(false); onNavigate(id); }}
        onAction={handlePaletteAction} />
    </div>
  );
}

Object.assign(window, { ToastContext, ToastProvider, useToast, Modal, ConfirmDialog,
  Sidebar, TopBar, AppLayout, IconBtn, CommandPalette });
