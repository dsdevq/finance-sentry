// Finance Sentry — Settings Page
const { useState } = React;

// ── Section wrapper ───────────────────────────────────────────────────────────
function SettingsSection({ title, description, children }) {
  return (
    <div style={{ display:'grid', gridTemplateColumns:'280px 1fr', gap:40, paddingBottom:36,
      borderBottom:'1px solid var(--border-default)' }}>
      <div>
        <h2 style={{ fontSize:'14px', fontWeight:600, color:'var(--text-primary)', marginBottom:6 }}>{title}</h2>
        {description && <p style={{ fontSize:'13px', color:'var(--text-secondary)', lineHeight:1.6, margin:0 }}>{description}</p>}
      </div>
      <div style={{ display:'flex', flexDirection:'column', gap:16 }}>{children}</div>
    </div>
  );
}

// ── Toggle ────────────────────────────────────────────────────────────────────
function Toggle({ checked, onChange, label, description }) {
  return (
    <div style={{ display:'flex', alignItems:'flex-start', justifyContent:'space-between', gap:20 }}>
      <div>
        <div style={{ fontSize:'14px', fontWeight:500, color:'var(--text-primary)', marginBottom:2 }}>{label}</div>
        {description && <div style={{ fontSize:'12px', color:'var(--text-secondary)', lineHeight:1.5 }}>{description}</div>}
      </div>
      <button onClick={() => onChange(!checked)} role="switch" aria-checked={checked}
        style={{ flexShrink:0, width:44, height:24, borderRadius:12, border:'none', cursor:'pointer',
          background: checked ? 'var(--accent-default)' : 'var(--border-default)',
          position:'relative', transition:'background 200ms', padding:0 }}>
        <div style={{ position:'absolute', top:3, left: checked ? 22 : 3, width:18, height:18,
          borderRadius:'50%', background:'white', transition:'left 200ms',
          boxShadow:'0 1px 4px rgba(0,0,0,.2)' }} />
      </button>
    </div>
  );
}

// ── Select ────────────────────────────────────────────────────────────────────
function Select({ value, onChange, options, label }) {
  return (
    <div style={{ display:'flex', flexDirection:'column', gap:5 }}>
      {label && <span style={{ fontSize:'11px', fontWeight:600, letterSpacing:'0.06em',
        textTransform:'uppercase', color:'var(--text-secondary)' }}>{label}</span>}
      <select value={value} onChange={e => onChange(e.target.value)}
        style={{ padding:'8px 12px', borderRadius:6, border:'1.5px solid var(--border-default)',
          background:'var(--surface-bg)', color:'var(--text-primary)', fontSize:'13px',
          outline:'none', cursor:'pointer', appearance:'none',
          backgroundImage:`url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 24 24' fill='none' stroke='%239a9aaa' stroke-width='2'%3E%3Cpath d='m6 9 6 6 6-6'/%3E%3C/svg%3E")`,
          backgroundRepeat:'no-repeat', backgroundPosition:'calc(100% - 10px) center',
          paddingRight:32 }}>
        {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
      </select>
    </div>
  );
}

// ── Provider card (in Settings) ───────────────────────────────────────────────
function ProviderCard({ accounts, onDisconnect, onConnect }) {
  const PROVIDER_META = {
    plaid:    { color:'#00D4AA', letter:'P', name:'Plaid',                canRevoke:true },
    monobank: { color:'#1b1b24', letter:'M', name:'Monobank',             canRevoke:true },
    ibkr:     { color:'#c8102e', letter:'IB', name:'Interactive Brokers', canRevoke:false },
    binance:  { color:'#F0B90B', letter:'B', name:'Binance',              canRevoke:true },
  };

  // Group by provider
  const grouped = accounts.reduce((acc, a) => {
    const key = a.provider;
    if (!acc[key]) acc[key] = [];
    acc[key].push(a);
    return acc;
  }, {});

  return (
    <div style={{ display:'flex', flexDirection:'column', gap:10 }}>
      {Object.entries(grouped).map(([pid, accs]) => {
        const meta = PROVIDER_META[pid] || { color:'var(--accent-default)', letter:pid[0].toUpperCase(), name:pid, canRevoke:true };
        return (
          <div key={pid} style={{ display:'flex', alignItems:'center', gap:14, padding:'14px 16px',
            background:'var(--surface-bg)', border:'1px solid var(--border-default)',
            borderRadius:10 }}>
            <div style={{ width:40, height:40, borderRadius:10, background:meta.color,
              display:'flex', alignItems:'center', justifyContent:'center',
              fontSize:'13px', fontWeight:800, color:'white', letterSpacing:'-0.02em', flexShrink:0 }}>
              {meta.letter}
            </div>
            <div style={{ flex:1 }}>
              <div style={{ fontSize:'14px', fontWeight:500, color:'var(--text-primary)', marginBottom:3 }}>{meta.name}</div>
              <div style={{ display:'flex', gap:8, flexWrap:'wrap' }}>
                {accs.map(a => (
                  <span key={a.accountId} style={{ fontSize:'11px', color:'var(--text-secondary)',
                    background:'var(--surface-raised)', padding:'2px 8px', borderRadius:4 }}>
                    {a.accountType} {a.accountNumberLast4 ? `····${a.accountNumberLast4}` : ''}
                  </span>
                ))}
              </div>
            </div>
            <StatusIndicator status={accs[0].syncStatus} timestamp={relativeTime(accs[0].lastSyncTs)} />
            {meta.canRevoke
              ? <Button variant="destructive" size="sm" icon="Unplug"
                  onClick={() => onDisconnect(accs[0])}>Revoke</Button>
              : <span style={{ fontSize:'12px', color:'var(--text-disabled)', whiteSpace:'nowrap' }}>Read-only</span>}
          </div>
        );
      })}
      <button onClick={onConnect}
        style={{ display:'flex', alignItems:'center', justifyContent:'center', gap:8,
          padding:'11px', borderRadius:10, border:'1.5px dashed var(--border-default)',
          background:'transparent', cursor:'pointer', fontSize:'13px', color:'var(--text-secondary)',
          fontFamily:'inherit', transition:'border-color 150ms, color 150ms' }}
        onMouseEnter={e=>{e.currentTarget.style.borderColor='var(--accent-default)';e.currentTarget.style.color='var(--accent-default)'}}
        onMouseLeave={e=>{e.currentTarget.style.borderColor='var(--border-default)';e.currentTarget.style.color='var(--text-secondary)'}}>
        <Icon name="Plus" size="xs" /> Add provider
      </button>
    </div>
  );
}

// ── Main Settings Page ────────────────────────────────────────────────────────
function SettingsPage({ accounts, onDisconnect, onConnect, onLogout }) {
  const { addToast } = useToast();

  const [profile, setProfile] = useState({ ...USER_PROFILE });
  const [saving, setSaving] = useState(false);
  const [pwForm, setPwForm] = useState({ current:'', next:'', confirm:'' });
  const [pwSaving, setPwSaving] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  const saveProfile = () => {
    setSaving(true);
    setTimeout(() => { setSaving(false); addToast('Profile saved successfully', 'success'); }, 900);
  };

  const changePassword = e => {
    e.preventDefault();
    if (!pwForm.current) { addToast('Enter your current password', 'error'); return; }
    if (pwForm.next.length < 8) { addToast('New password must be at least 8 characters', 'error'); return; }
    if (pwForm.next !== pwForm.confirm) { addToast('Passwords do not match', 'error'); return; }
    setPwSaving(true);
    setTimeout(() => {
      setPwSaving(false);
      setPwForm({ current:'', next:'', confirm:'' });
      addToast('Password updated', 'success');
    }, 1100);
  };

  return (
    <div style={{ padding:'32px 24px' }}>
      <div style={{ maxWidth:900, margin:'0 auto', display:'flex', flexDirection:'column', gap:40 }}>

        {/* Profile */}
        <SettingsSection title="Profile" description="Your name and contact information shown throughout Finance Sentry.">
          <div style={{ display:'flex', alignItems:'center', gap:16, marginBottom:4 }}>
            <div style={{ width:56, height:56, borderRadius:'50%', background:'var(--accent-default)',
              display:'flex', alignItems:'center', justifyContent:'center',
              fontSize:'18px', fontWeight:700, color:'white', flexShrink:0 }}>
              {profile.firstName[0]}{profile.lastName[0]}
            </div>
            <div>
              <div style={{ fontSize:'15px', fontWeight:600, color:'var(--text-primary)' }}>
                {profile.firstName} {profile.lastName}
              </div>
              <div style={{ fontSize:'13px', color:'var(--text-secondary)' }}>{profile.email}</div>
            </div>
          </div>
          <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:14 }}>
            <FormField label="First Name" id="s-first">
              <Input id="s-first" value={profile.firstName}
                onChange={e => setProfile(p => ({...p, firstName:e.target.value}))} />
            </FormField>
            <FormField label="Last Name" id="s-last">
              <Input id="s-last" value={profile.lastName}
                onChange={e => setProfile(p => ({...p, lastName:e.target.value}))} />
            </FormField>
          </div>
          <FormField label="Email Address" id="s-email">
            <Input id="s-email" type="email" value={profile.email}
              onChange={e => setProfile(p => ({...p, email:e.target.value}))} />
          </FormField>
          <div style={{ display:'flex', justifyContent:'flex-end' }}>
            <Button onClick={saveProfile} loading={saving}>Save Profile</Button>
          </div>
        </SettingsSection>

        {/* Preferences */}
        <SettingsSection title="Preferences" description="Display settings and regional configuration.">
          <Select label="Base Currency" value={profile.baseCurrency}
            onChange={v => setProfile(p => ({...p, baseCurrency:v}))}
            options={[
              {value:'USD', label:'USD — US Dollar'},
              {value:'EUR', label:'EUR — Euro'},
              {value:'GBP', label:'GBP — British Pound'},
              {value:'UAH', label:'UAH — Ukrainian Hryvnia'},
              {value:'BTC', label:'BTC — Bitcoin'},
            ]} />
          <Select label="Theme" value={profile.theme}
            onChange={v => setProfile(p => ({...p, theme:v}))}
            options={[
              {value:'system', label:'System default'},
              {value:'light',  label:'Light'},
              {value:'dark',   label:'Dark'},
            ]} />
        </SettingsSection>

        {/* Notifications */}
        <SettingsSection title="Notifications" description="Control when and how Finance Sentry alerts you.">
          <Toggle label="Email alerts" description="Receive weekly digest and important account notifications."
            checked={profile.emailAlerts} onChange={v => setProfile(p=>({...p,emailAlerts:v}))} />
          <Toggle label="Low balance warnings" description="Get notified when an account drops below your threshold."
            checked={profile.lowBalanceThreshold > 0}
            onChange={v => setProfile(p=>({...p, lowBalanceThreshold:v?500:0}))} />
          {profile.lowBalanceThreshold > 0 && (
            <div style={{ paddingLeft:0 }}>
              <FormField label="Low balance threshold (USD)" id="s-thresh">
                <Input id="s-thresh" type="text" value={String(profile.lowBalanceThreshold)}
                  onChange={e => setProfile(p=>({...p,lowBalanceThreshold:Number(e.target.value)||0}))} />
              </FormField>
            </div>
          )}
          <Toggle label="Sync failure alerts" description="Notify when a provider fails to sync for more than 24 hours."
            checked={true} onChange={() => addToast('This would update notification settings', 'info')} />
        </SettingsSection>

        {/* Security */}
        <SettingsSection title="Security" description="Manage your password and two-factor authentication.">
          <Toggle label="Two-factor authentication"
            description="Add an extra layer of security to your account with an authenticator app."
            checked={profile.twoFactor}
            onChange={v => {
              setProfile(p=>({...p,twoFactor:v}));
              addToast(v ? '2FA enabled (mock)' : '2FA disabled', v?'success':'warning');
            }} />
          <form onSubmit={changePassword} style={{ display:'flex', flexDirection:'column', gap:12 }}>
            <div style={{ fontSize:'11px', fontWeight:600, letterSpacing:'0.07em',
              textTransform:'uppercase', color:'var(--text-secondary)', marginBottom:2 }}>Change Password</div>
            <FormField label="Current Password" id="s-cpw">
              <Input id="s-cpw" type="password" value={pwForm.current} placeholder="••••••••"
                onChange={e=>setPwForm(p=>({...p,current:e.target.value}))} />
            </FormField>
            <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:12 }}>
              <FormField label="New Password" id="s-npw">
                <Input id="s-npw" type="password" value={pwForm.next} placeholder="Min 8 chars"
                  onChange={e=>setPwForm(p=>({...p,next:e.target.value}))} />
              </FormField>
              <FormField label="Confirm Password" id="s-rpw">
                <Input id="s-rpw" type="password" value={pwForm.confirm} placeholder="Repeat"
                  onChange={e=>setPwForm(p=>({...p,confirm:e.target.value}))} />
              </FormField>
            </div>
            <div style={{ display:'flex', justifyContent:'flex-end' }}>
              <Button type="submit" variant="secondary" loading={pwSaving} icon="Key">Update Password</Button>
            </div>
          </form>
        </SettingsSection>

        {/* Connected providers */}
        <SettingsSection title="Connected Providers" description="Manage the data sources Finance Sentry syncs with. Revoking a connection stops future syncs but preserves historical data.">
          <ProviderCard accounts={accounts} onDisconnect={onDisconnect} onConnect={onConnect} />
        </SettingsSection>

        {/* Danger zone */}
        <div style={{ padding:'22px', borderRadius:10, border:'1.5px solid rgba(239,68,68,.3)',
          background:'rgba(239,68,68,.04)', display:'flex', alignItems:'center', justifyContent:'space-between', gap:20 }}>
          <div>
            <div style={{ fontSize:'14px', fontWeight:600, color:'var(--status-error)', marginBottom:4 }}>Danger Zone</div>
            <div style={{ fontSize:'13px', color:'var(--text-secondary)', lineHeight:1.5 }}>
              Permanently delete your account and all associated data. This cannot be undone.
            </div>
          </div>
          <Button variant="destructive" size="sm" onClick={() => setShowDeleteConfirm(true)}>Delete Account</Button>
        </div>

        {/* Bottom sign-out */}
        <div style={{ display:'flex', justifyContent:'flex-end', paddingBottom:24 }}>
          <Button variant="ghost" icon="LogOut" onClick={onLogout}>Sign out</Button>
        </div>
      </div>

      <ConfirmDialog open={showDeleteConfirm} onClose={() => setShowDeleteConfirm(false)}
        onConfirm={() => { setShowDeleteConfirm(false); addToast('Account deletion requested (mock)', 'warning'); }}
        confirmVariant="destructive" confirmLabel="Delete Forever"
        title="Delete your account?"
        message="All your connected accounts, transactions, and settings will be permanently erased. This action cannot be reversed." />
    </div>
  );
}

Object.assign(window, { SettingsPage });
