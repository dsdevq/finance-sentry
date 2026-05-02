// Finance Sentry — Auth Pages (Login + Register)
const { useState } = React;

function PasswordStrength({ password }) {
  if (!password) return null;
  const score = [/.{8,}/, /[A-Z]/, /[0-9]/, /[^A-Za-z0-9]/].filter(r => r.test(password)).length;
  const labels = ['','Weak','Fair','Good','Strong'];
  const colors = ['','var(--status-error)','var(--status-warning)','var(--status-info)','var(--status-success)'];
  return (
    <div style={{ marginTop:8 }}>
      <div style={{ display:'flex', gap:4, marginBottom:5 }}>
        {[1,2,3,4].map(i => (
          <div key={i} style={{ height:3, flex:1, borderRadius:2,
            background: i<=score ? colors[score] : 'var(--border-default)',
            transition:'background 200ms' }} />
        ))}
      </div>
      <span style={{ fontSize:'11px', color:colors[score] }}>{labels[score]}</span>
    </div>
  );
}

function GoogleBtn() {
  return (
    <button style={{ display:'flex', alignItems:'center', justifyContent:'center', gap:10,
      width:'100%', padding:'10px 16px', background:'var(--surface-card)',
      border:'1.5px solid var(--border-default)', borderRadius:8, cursor:'pointer',
      fontSize:'14px', fontWeight:500, color:'var(--text-primary)', fontFamily:'inherit',
      transition:'background 120ms, border-color 120ms' }}
      onMouseEnter={e=>{e.currentTarget.style.background='var(--surface-raised)';e.currentTarget.style.borderColor='var(--border-strong)'}}
      onMouseLeave={e=>{e.currentTarget.style.background='var(--surface-card)';e.currentTarget.style.borderColor='var(--border-default)'}}>
      <svg width="18" height="18" viewBox="0 0 24 24">
        <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
        <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
        <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
        <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
      </svg>
      Continue with Google
    </button>
  );
}

function Divider({ label }) {
  return (
    <div style={{ display:'flex', alignItems:'center', gap:12, margin:'20px 0' }}>
      <div style={{ flex:1, height:1, background:'var(--border-default)' }} />
      <span style={{ fontSize:'11px', letterSpacing:'0.06em', textTransform:'uppercase',
        color:'var(--text-disabled)' }}>{label}</span>
      <div style={{ flex:1, height:1, background:'var(--border-default)' }} />
    </div>
  );
}

// ── Login Page ────────────────────────────────────────────────────────────────
function LoginPage({ onLogin, onGoRegister }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = e => {
    e.preventDefault();
    if (!email || !password) { setError('Please fill in all fields.'); return; }
    setError(''); setLoading(true);
    setTimeout(() => { setLoading(false); onLogin(); }, 1400);
  };

  return (
    <div style={{ minHeight:'100%', display:'flex', alignItems:'center', justifyContent:'center',
      background:'var(--surface-bg)', padding:24 }}>
      <div style={{ width:'100%', maxWidth:440 }}>
        {/* Brand mark */}
        <div style={{ display:'flex', flexDirection:'column', alignItems:'center', marginBottom:32 }}>
          <div style={{ width:48, height:48, borderRadius:14, background:'var(--accent-default)',
            display:'flex', alignItems:'center', justifyContent:'center', marginBottom:12,
            boxShadow:'0 8px 24px rgba(79,70,229,0.3)' }}>
            <Icon name="ShieldCheck" size="md" style={{ color:'white' }} />
          </div>
          <span style={{ fontSize:'13px', color:'var(--text-secondary)', letterSpacing:'0.06em',
            textTransform:'uppercase', fontWeight:600 }}>Finance Sentry</span>
        </div>

        <Card elevated style={{ padding:'32px' }}>
          <h1 style={{ fontSize:'22px', fontWeight:700, color:'var(--text-primary)', marginBottom:6 }}>
            Secure Access
          </h1>
          <p style={{ fontSize:'13px', color:'var(--text-secondary)', marginBottom:24, lineHeight:1.5 }}>
            Enter your credentials to manage your financial accounts.
          </p>

          {error && <div style={{ marginBottom:16 }}><Alert variant="error">{error}</Alert></div>}

          <form onSubmit={handleSubmit} style={{ display:'flex', flexDirection:'column', gap:16 }}>
            <FormField label="Email Address" id="email">
              <Input id="email" type="email" value={email} onChange={e=>setEmail(e.target.value)}
                placeholder="name@organization.com" />
            </FormField>

            <FormField id="password" label={
              <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between' }}>
                <span>Password</span>
                <a href="#" style={{ fontSize:'11px', color:'var(--accent-default)', textDecoration:'none',
                  fontWeight:500, textTransform:'none', letterSpacing:0 }}>Forgot?</a>
              </div>
            }>
              <Input id="password" type="password" value={password} onChange={e=>setPassword(e.target.value)}
                placeholder="••••••••" />
            </FormField>

            <Button type="submit" fullWidth loading={loading} style={{ marginTop:4 }}>
              {loading ? 'Authenticating…' : 'Authenticate →'}
            </Button>
          </form>

          <Divider label="or SSO" />
          <GoogleBtn />

          <p style={{ textAlign:'center', marginTop:20, fontSize:'13px', color:'var(--text-secondary)' }}>
            No account?{' '}
            <a href="#" onClick={e=>{e.preventDefault();onGoRegister()}}
              style={{ color:'var(--accent-default)', fontWeight:500 }}>Create one</a>
          </p>
        </Card>

        <p style={{ textAlign:'center', marginTop:16, fontSize:'11px', color:'var(--text-disabled)' }}>
          Protected by 256-bit AES encryption · SOC 2 Type II certified
        </p>
      </div>
    </div>
  );
}

// ── Register Page ─────────────────────────────────────────────────────────────
function RegisterPage({ onRegister, onGoLogin }) {
  const [first, setFirst] = useState('');
  const [last, setLast] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [agreed, setAgreed] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = e => {
    e.preventDefault();
    if (!first||!last||!email||!password) { setError('Please fill in all fields.'); return; }
    if (!agreed) { setError('You must agree to the Terms of Service.'); return; }
    setError(''); setLoading(true);
    setTimeout(() => { setLoading(false); onRegister(); }, 1600);
  };

  return (
    <div style={{ minHeight:'100%', display:'flex', alignItems:'center', justifyContent:'center',
      background:'var(--surface-bg)', padding:24 }}>
      <div style={{ width:'100%', maxWidth:480 }}>
        <div style={{ display:'flex', flexDirection:'column', alignItems:'center', marginBottom:28 }}>
          <div style={{ width:48, height:48, borderRadius:14, background:'var(--accent-default)',
            display:'flex', alignItems:'center', justifyContent:'center', marginBottom:12,
            boxShadow:'0 8px 24px rgba(79,70,229,0.3)' }}>
            <Icon name="ShieldCheck" size="md" style={{ color:'white' }} />
          </div>
          <span style={{ fontSize:'13px', color:'var(--text-secondary)', letterSpacing:'0.06em',
            textTransform:'uppercase', fontWeight:600 }}>Finance Sentry</span>
        </div>

        <Card elevated style={{ padding:0, overflow:'hidden' }}>
          <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between',
            padding:'14px 28px', borderBottom:'1px solid var(--border-default)' }}>
            <span style={{ fontSize:'13px', color:'var(--text-secondary)' }}>Already have an account?</span>
            <a href="#" onClick={e=>{e.preventDefault();onGoLogin()}}
              style={{ fontSize:'13px', color:'var(--accent-default)', fontWeight:500 }}>Sign In</a>
          </div>

          <div style={{ padding:'28px' }}>
            <h1 style={{ fontSize:'22px', fontWeight:700, color:'var(--text-primary)', marginBottom:6 }}>Get Started</h1>
            <p style={{ fontSize:'13px', color:'var(--text-secondary)', marginBottom:22, lineHeight:1.5 }}>
              Create an account to begin your journey to financial clarity.
            </p>

            <div style={{ marginBottom:20 }}><GoogleBtn /></div>
            <Divider label="or continue with email" />

            {error && <div style={{ marginBottom:16 }}><Alert variant="error">{error}</Alert></div>}

            <form onSubmit={handleSubmit} style={{ display:'flex', flexDirection:'column', gap:14 }}>
              <div style={{ display:'flex', gap:14 }}>
                <FormField label="First Name" id="first" style={{ flex:1 }}>
                  <Input id="first" value={first} onChange={e=>setFirst(e.target.value)} placeholder="John" />
                </FormField>
                <FormField label="Last Name" id="last" style={{ flex:1 }}>
                  <Input id="last" value={last} onChange={e=>setLast(e.target.value)} placeholder="Doe" />
                </FormField>
              </div>

              <FormField label="Email Address" id="reg-email">
                <Input id="reg-email" type="email" value={email} onChange={e=>setEmail(e.target.value)}
                  placeholder="name@organization.com" />
              </FormField>

              <FormField label="Password" id="reg-pw">
                <Input id="reg-pw" type="password" value={password} onChange={e=>setPassword(e.target.value)}
                  placeholder="Min. 8 characters" />
                <PasswordStrength password={password} />
              </FormField>

              <label style={{ display:'flex', alignItems:'flex-start', gap:10, cursor:'pointer', marginTop:4 }}>
                <input type="checkbox" checked={agreed} onChange={e=>setAgreed(e.target.checked)}
                  style={{ marginTop:2, width:15, height:15, accentColor:'var(--accent-default)' }} />
                <span style={{ fontSize:'13px', color:'var(--text-secondary)', lineHeight:1.5 }}>
                  I agree to the{' '}
                  <a href="#" style={{ color:'var(--accent-default)' }}>Terms of Service</a>
                  {' '}and{' '}
                  <a href="#" style={{ color:'var(--accent-default)' }}>Privacy Policy</a>
                </span>
              </label>

              <Button type="submit" fullWidth loading={loading} style={{ marginTop:4 }}>
                {loading ? 'Creating account…' : 'Create Account'}
              </Button>
            </form>
          </div>
        </Card>
      </div>
    </div>
  );
}

Object.assign(window, { LoginPage, RegisterPage });
