/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/**/*.{ts,html}',
    './projects/**/*.{ts,html}',
  ],
  // Attribute-based dark mode: data-theme="dark" on <html>
  darkMode: ['selector', '[data-theme="dark"]'],
  theme: {
    extend: {
      // ─── Surface ───────────────────────────────────────────────
      colors: {
        'surface-bg':       'var(--color-surface-bg)',
        'surface-card':     'var(--color-surface-card)',
        'surface-raised':   'var(--color-surface-raised)',

        // ─── Text ───────────────────────────────────────────────
        'text-primary':     'var(--color-text-primary)',
        'text-secondary':   'var(--color-text-secondary)',
        'text-disabled':    'var(--color-text-disabled)',
        'text-inverse':     'var(--color-text-inverse)',

        // ─── Accent ─────────────────────────────────────────────
        'accent-100':       'var(--color-accent-100)',
        'accent-200':       'var(--color-accent-200)',
        'accent-300':       'var(--color-accent-300)',
        'accent-400':       'var(--color-accent-400)',
        'accent-500':       'var(--color-accent-500)',
        'accent-600':       'var(--color-accent-600)',
        'accent-700':       'var(--color-accent-700)',
        'accent-800':       'var(--color-accent-800)',
        'accent-900':       'var(--color-accent-900)',
        'accent-1000':      'var(--color-accent-1000)',
        'accent-default':   'var(--color-accent-default)',
        'accent-hover':     'var(--color-accent-hover)',
        'accent-active':    'var(--color-accent-active)',
        'accent-subtle':    'var(--color-accent-subtle)',

        // ─── Status ─────────────────────────────────────────────
        'status-info':      'var(--color-status-info)',
        'status-success':   'var(--color-status-success)',
        'status-warning':   'var(--color-status-warning)',
        'status-error':     'var(--color-status-error)',

        // ─── Border ─────────────────────────────────────────────
        'border-default':   'var(--color-border-default)',
        'border-strong':    'var(--color-border-strong)',
        'border-focus':     'var(--color-border-focus)',
      },

      // ─── Font families ──────────────────────────────────────────
      fontFamily: {
        base:     ['Inter', 'system-ui', 'sans-serif'],
        headline: ['Inter', 'system-ui', 'sans-serif'],
        label:    ['Inter', 'system-ui', 'sans-serif'],
        mono:     ['ui-monospace', '"SFMono-Regular"', '"Fira Code"', 'monospace'],
      },

      // ─── Font sizes ─────────────────────────────────────────────
      fontSize: {
        'cmn-xs':   ['0.75rem',  { lineHeight: '1.25' }],
        'cmn-sm':   ['0.875rem', { lineHeight: '1.5'  }],
        'cmn-md':   ['1rem',     { lineHeight: '1.5'  }],
        'cmn-lg':   ['1.125rem', { lineHeight: '1.5'  }],
        'cmn-xl':   ['1.25rem',  { lineHeight: '1.25' }],
        'cmn-2xl':  ['1.5rem',   { lineHeight: '1.25' }],
        'cmn-3xl':  ['1.875rem', { lineHeight: '1.25' }],
        'cmn-4xl':  ['2.25rem',  { lineHeight: '1.1'  }],
      },

      // ─── Border radius ──────────────────────────────────────────
      borderRadius: {
        'cmn-sm':   '0.25rem',
        'cmn-md':   '0.5rem',
        'cmn-lg':   '0.75rem',
        'cmn-full': '9999px',
      },

      // ─── Box shadows ────────────────────────────────────────────
      boxShadow: {
        'cmn-sm': '0 2px 8px rgba(25, 28, 29, 0.04)',
        'cmn-md': '0 12px 40px rgba(25, 28, 29, 0.06)',
        'cmn-lg': '0 24px 64px rgba(25, 28, 29, 0.10)',
      },

      // ─── Spacing ────────────────────────────────────────────────
      spacing: {
        'cmn-1':  '4px',
        'cmn-2':  '8px',
        'cmn-3':  '12px',
        'cmn-4':  '16px',
        'cmn-6':  '24px',
        'cmn-8':  '32px',
        'cmn-12': '48px',
      },
    },
  },
  plugins: [],
};
