// Finance Sentry — Root App + Connect Modal + Routing
const { useState, useEffect } = React;

// ── Connect Modal ─────────────────────────────────────────────────────────────
function ConnectModal({ open, onClose, onConnected }) {
  const [step, setStep] = useState('pick'); // pick | form | loading | success
  const [provider, setProvider] = useState(null);
  const [fields, setFields] = useState({});
  const { addToast } = useToast();

  const reset = () => { setStep('pick'); setProvider(null); setFields({}); };
  const handleClose = () => { reset(); onClose(); };

  const selectProvider = p => { setProvider(p); setFields({}); setStep('form'); };

  const handleConnect = () => {
    setStep('loading');
    setTimeout(() => {
      setStep('success');
      const newAccount = {
        accountId: 'new-' + Date.now(),
        bankName: provider.name,
        accountType: provider.id === 'binance' ? 'Spot' : provider.id === 'ibkr' ? 'Brokerage' : 'Checking',
        accountNumberLast4: Math.floor(1000 + Math.random() * 9000).toString(),
        currentBalance: Math.random() * 10000 + 500,
        balanceUsd: Math.random() * 10000 + 500,
        currency: 'USD',
        syncStatus: 'synced',
        lastSyncTs: Date.now(),
        provider: provider.id,
        category: provider.id === 'binance' ? 'crypto' : provider.id === 'ibkr' ? 'brokerage' : 'banking',
      };
      onConnected(newAccount);
    }, 2200);
  };

  const ProviderLogo = ({ id }) => {
    const colors = { plaid:'#00D4AA', monobank:'#1b1b24', ibkr:'#c8102e', binance:'#F0B90B' };
    const letters = { plaid:'P', monobank:'M', ibkr:'IB', binance:'B' };
    return (
      <div style={{ width:44, height:44, borderRadius:12, background:colors[id]||'var(--accent-default)',
        display:'flex', alignItems:'center', justifyContent:'center', fontSize:'15px', fontWeight:800,
        color:'white', letterSpacing:'-0.02em' }}>{letters[id]||'?'}</div>
    );
  };

  const body = () => {
    if (step === 'pick') return (
      <div style={{ display:'flex', flexDirection:'column', gap:10 }}>
        <p style={{ fontSize:'13px', color:'var(--text-secondary)', marginBottom:6, lineHeight:1.5 }}>
          Select a provider to connect your financial accounts securely.
        </p>
        {PROVIDERS_CONFIG.map(p => (
          <button key={p.id} onClick={() => selectProvider(p)}
            style={{ display:'flex', alignItems:'center', gap:16, padding:'16px',
              background:'var(--surface-bg)', border:'1.5px solid var(--border-default)',
              borderRadius:10, cursor:'pointer', textAlign:'left', fontFamily:'inherit',
              transition:'border-color 150ms, background 150ms', width:'100%' }}
            onMouseEnter={e=>{e.currentTarget.style.borderColor='var(--accent-default)';e.currentTarget.style.background='var(--accent-subtle)'}}
            onMouseLeave={e=>{e.currentTarget.style.borderColor='var(--border-default)';e.currentTarget.style.background='var(--surface-bg)'}}>
            <ProviderLogo id={p.id} />
            <div style={{ flex:1 }}>
              <div style={{ fontSize:'14px', fontWeight:600, color:'var(--text-primary)', marginBottom:3 }}>{p.name}</div>
              <div style={{ fontSize:'12px', color:'var(--text-secondary)', lineHeight:1.4 }}>{p.description}</div>
            </div>
            <Icon name="ChevronRight" size="sm" style={{ color:'var(--text-disabled)', flexShrink:0 }} />
          </button>
        ))}
      </div>
    );

    if (step === 'form') return (
      <div>
        <button onClick={() => setStep('pick')}
          style={{ display:'flex', alignItems:'center', gap:6, marginBottom:20, background:'none',
            border:'none', cursor:'pointer', fontSize:'13px', color:'var(--text-secondary)',
            padding:0, fontFamily:'inherit' }}
          onMouseEnter={e=>e.currentTarget.style.color='var(--text-primary)'}
          onMouseLeave={e=>e.currentTarget.style.color='var(--text-secondary)'}>
          <Icon name="ChevronLeft" size="xs" /> Back to providers
        </button>
        <div style={{ display:'flex', alignItems:'center', gap:14, marginBottom:22,
          padding:16, background:'var(--surface-bg)', borderRadius:10, border:'1px solid var(--border-default)' }}>
          <ProviderLogo id={provider.id} />
          <div>
            <div style={{ fontSize:'14px', fontWeight:600, color:'var(--text-primary)' }}>{provider.name}</div>
            <div style={{ fontSize:'12px', color:'var(--text-secondary)' }}>{provider.description}</div>
          </div>
        </div>
        <div style={{ display:'flex', flexDirection:'column', gap:14 }}>
          {provider.fields.map(f => f.type === 'info' ? (
            <Alert key={f.id} variant="info">{f.text}</Alert>
          ) : (
            <FormField key={f.id} label={f.label} id={f.id}>
              <Input id={f.id} type={f.type} placeholder={f.placeholder}
                value={fields[f.id]||''} onChange={e=>setFields(v=>({...v,[f.id]:e.target.value}))} />
            </FormField>
          ))}
        </div>
      </div>
    );

    if (step === 'loading') return (
      <div style={{ textAlign:'center', padding:'40px 20px' }}>
        <div style={{ width:56, height:56, borderRadius:'50%', background:'var(--accent-subtle)',
          display:'flex', alignItems:'center', justifyContent:'center', margin:'0 auto 20px' }}>
          <Icon name="Loader2" size="lg" style={{ color:'var(--accent-default)', animation:'spin 0.8s linear infinite' }} />
        </div>
        <div style={{ fontSize:'16px', fontWeight:600, color:'var(--text-primary)', marginBottom:8 }}>
          Connecting to {provider.name}…
        </div>
        <div style={{ fontSize:'13px', color:'var(--text-secondary)', lineHeight:1.5 }}>
          Verifying credentials and fetching account data.<br />This usually takes a few seconds.
        </div>
      </div>
    );

    if (step === 'success') return (
      <div style={{ textAlign:'center', padding:'36px 20px' }}>
        <div style={{ width:60, height:60, borderRadius:'50%', background:'rgba(16,185,129,.12)',
          display:'flex', alignItems:'center', justifyContent:'center', margin:'0 auto 20px' }}>
          <Icon name="CheckCircle2" size="lg" style={{ color:'var(--status-success)' }} />
        </div>
        <div style={{ fontSize:'17px', fontWeight:700, color:'var(--text-primary)', marginBottom:8 }}>
          {provider.name} connected!
        </div>
        <div style={{ fontSize:'13px', color:'var(--text-secondary)', marginBottom:28, lineHeight:1.5 }}>
          Your account has been linked successfully.<br />Initial sync is in progress.
        </div>
        <Button onClick={handleClose}>Done</Button>
      </div>
    );
  };

  const footer = () => {
    if (step === 'form') return (
      <>
        <Button variant="secondary" onClick={() => setStep('pick')}>Cancel</Button>
        <Button onClick={handleConnect} icon="Link" iconPos="prefix">Connect {provider.name}</Button>
      </>
    );
    if (step === 'pick' || step === 'success') return null;
    return null;
  };

  return (
    <Modal open={open} onClose={step==='loading'?undefined:handleClose}
      title={step==='pick' ? 'Connect Account' : step==='form' ? `Connect ${provider?.name}` : step==='loading' ? 'Connecting…' : 'Connected!'}
      footer={footer()}>
      {body()}
    </Modal>
  );
}

// ── Root App ──────────────────────────────────────────────────────────────────
function App() {
  const savedDark = localStorage.getItem('fs-dark') === 'true';
  const [dark, setDark] = useState(savedDark);
  const [page, setPage] = useState('login'); // login | register | dashboard | accounts | transactions | holdings
  const [accounts, setAccounts] = useState(INIT_ACCOUNTS);
  const [selectedAccount, setSelectedAccount] = useState(null);
  const [showConnect, setShowConnect] = useState(false);
  const [disconnectTarget, setDisconnectTarget] = useState(null);
  const [disconnecting, setDisconnecting] = useState(false);

  // Apply theme to DOM
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
    localStorage.setItem('fs-dark', dark);
  }, [dark]);

  const { addToast } = useToast();

  const navigate = p => {
    if (p === 'login') { setPage('login'); return; }
    setPage(p);
  };

  const handleLogin = () => setPage('dashboard');
  const handleRegister = () => setPage('dashboard');

  const handleViewTransactions = acc => {
    setSelectedAccount(acc);
    setPage('transactions');
  };

  const handleConnected = newAcc => {
    setAccounts(prev => [...prev, newAcc]);
    setTimeout(() => {
      addToast(`${newAcc.bankName} connected successfully`, 'success');
    }, 300);
  };

  const handleDisconnect = acc => setDisconnectTarget(acc);

  const confirmDisconnect = () => {
    setDisconnecting(true);
    setTimeout(() => {
      setAccounts(prev => prev.filter(a => a.accountId !== disconnectTarget.accountId));
      addToast(`${disconnectTarget.bankName} disconnected`, 'warning');
      setDisconnectTarget(null);
      setDisconnecting(false);
    }, 900);
  };

  // Auth pages
  if (page === 'login') return (
    <LoginPage onLogin={handleLogin} onGoRegister={() => setPage('register')} />
  );
  if (page === 'register') return (
    <RegisterPage onRegister={handleRegister} onGoLogin={() => setPage('login')} />
  );

  // App shell
  const pageContent = () => {
    switch (page) {
      case 'dashboard':    return <DashboardPage accounts={accounts} />;
      case 'accounts':     return <AccountsListPage accounts={accounts} onConnect={() => setShowConnect(true)} onDisconnect={handleDisconnect} onViewTransactions={handleViewTransactions} />;
      case 'transactions': return <TransactionListPage account={selectedAccount} onBack={() => setPage('accounts')} />;
      case 'holdings':     return <HoldingsPage accounts={accounts} onDisconnect={handleDisconnect} />;
      case 'budgets':      return <BudgetPage />;
      case 'subscriptions':return <SubscriptionsPage />;
      case 'settings':     return <SettingsPage accounts={accounts} onDisconnect={handleDisconnect} onConnect={() => setShowConnect(true)} onLogout={() => setPage('login')} />;
      default:             return <DashboardPage accounts={accounts} />;
    }
  };

  return (
    <>
      <AppLayout activePage={page} dark={dark} onToggleDark={() => setDark(d=>!d)}
        onNavigate={navigate} onConnect={() => setShowConnect(true)}>
        {pageContent()}
      </AppLayout>

      <ConnectModal open={showConnect} onClose={() => setShowConnect(false)} onConnected={handleConnected} />

      <ConfirmDialog
        open={!!disconnectTarget}
        onClose={() => setDisconnectTarget(null)}
        onConfirm={confirmDisconnect}
        loading={disconnecting}
        confirmVariant="destructive"
        confirmLabel="Disconnect"
        title={`Disconnect ${disconnectTarget?.bankName}?`}
        message={`This will remove ${disconnectTarget?.bankName} from Finance Sentry. Your historical transaction data will be preserved, but no new data will sync. You can reconnect at any time.`}
      />
    </>
  );
}

// ── Bootstrap ─────────────────────────────────────────────────────────────────
const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <ToastProvider>
    <App />
  </ToastProvider>
);
