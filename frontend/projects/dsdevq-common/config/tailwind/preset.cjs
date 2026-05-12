/** @type {Partial<import('tailwindcss').Config>} */
module.exports = {
  darkMode: ['selector', '[data-theme="dark"]'],
  theme: {
    extend: {
      colors: {
        'surface-bg': 'var(--color-surface-bg)',
        'surface-card': 'var(--color-surface-card)',
        'surface-raised': 'var(--color-surface-raised)',

        'text-primary': 'var(--color-text-primary)',
        'text-secondary': 'var(--color-text-secondary)',
        'text-disabled': 'var(--color-text-disabled)',
        'text-inverse': 'var(--color-text-inverse)',

        'accent-100': 'var(--color-accent-100)',
        'accent-200': 'var(--color-accent-200)',
        'accent-300': 'var(--color-accent-300)',
        'accent-400': 'var(--color-accent-400)',
        'accent-500': 'var(--color-accent-500)',
        'accent-600': 'var(--color-accent-600)',
        'accent-700': 'var(--color-accent-700)',
        'accent-800': 'var(--color-accent-800)',
        'accent-900': 'var(--color-accent-900)',
        'accent-1000': 'var(--color-accent-1000)',
        'accent-default': 'var(--color-accent-default)',
        'accent-hover': 'var(--color-accent-hover)',
        'accent-active': 'var(--color-accent-active)',
        'accent-subtle': 'var(--color-accent-subtle)',

        'status-info': 'var(--color-status-info)',
        'status-success': 'var(--color-status-success)',
        'status-warning': 'var(--color-status-warning)',
        'status-error': 'var(--color-status-error)',

        'border-default': 'var(--color-border-default)',
        'border-strong': 'var(--color-border-strong)',
        'border-focus': 'var(--color-border-focus)',
      },

      fontFamily: {
        base: ['Inter', 'system-ui', 'sans-serif'],
        headline: ['Inter', 'system-ui', 'sans-serif'],
        label: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['ui-monospace', '"SFMono-Regular"', '"Fira Code"', 'monospace'],
      },

      fontSize: {
        'cmn-xs': ['0.75rem', {lineHeight: '1.25'}],
        'cmn-sm': ['0.875rem', {lineHeight: '1.5'}],
        'cmn-md': ['1rem', {lineHeight: '1.5'}],
        'cmn-lg': ['1.125rem', {lineHeight: '1.5'}],
        'cmn-xl': ['1.25rem', {lineHeight: '1.25'}],
        'cmn-2xl': ['1.5rem', {lineHeight: '1.25'}],
        'cmn-3xl': ['1.875rem', {lineHeight: '1.25'}],
        'cmn-4xl': ['2.25rem', {lineHeight: '1.1'}],
      },

      borderRadius: {
        'cmn-sm': '0.25rem',
        'cmn-md': '0.5rem',
        'cmn-lg': '0.75rem',
        'cmn-full': '9999px',
      },

      boxShadow: {
        'cmn-sm': 'var(--shadow-sm)',
        'cmn-md': 'var(--shadow-md)',
        'cmn-lg': '0 24px 64px rgba(25, 28, 29, 0.10)',
      },

      keyframes: {
        'cmn-spin': {to: {transform: 'rotate(360deg)'}},
        'cmn-pulse': {
          '0%, 100%': {opacity: '1'},
          '50%': {opacity: '0.45'},
        },
        'cmn-fade-in': {
          from: {opacity: '0'},
          to: {opacity: '1'},
        },
        'cmn-slide-in': {
          from: {transform: 'translateY(10px) scale(0.98)', opacity: '0'},
          to: {transform: 'translateY(0) scale(1)', opacity: '1'},
        },
        'cmn-slide-in-right': {
          from: {transform: 'translateX(16px)', opacity: '0'},
          to: {transform: 'translateX(0)', opacity: '1'},
        },
      },

      animation: {
        'cmn-spin': 'cmn-spin 1s linear infinite',
        'cmn-pulse': 'cmn-pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'cmn-fade-in': 'cmn-fade-in 200ms ease-out',
        'cmn-slide-in': 'cmn-slide-in 240ms cubic-bezier(0.16, 1, 0.3, 1)',
        'cmn-slide-in-right': 'cmn-slide-in-right 220ms cubic-bezier(0.16, 1, 0.3, 1)',
      },

      spacing: {
        'cmn-1': '4px',
        'cmn-2': '8px',
        'cmn-3': '12px',
        'cmn-4': '16px',
        'cmn-5': '20px',
        'cmn-6': '24px',
        'cmn-7': '28px',
        'cmn-8': '32px',
        'cmn-10': '40px',
        'cmn-12': '48px',
        'cmn-16': '64px',
      },
    },
  },
  plugins: [],
};
