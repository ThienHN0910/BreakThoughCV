/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        white: 'var(--btc-surface)',
        black: 'var(--btc-ink)',
        slate: {
          50: 'var(--btc-bg-1)',
          100: 'var(--btc-bg-2)',
          200: 'var(--btc-border)',
          300: 'var(--btc-border)',
          400: 'var(--btc-muted)',
          500: 'var(--btc-muted)',
          600: 'var(--btc-muted)',
          700: 'var(--btc-ink)',
          800: 'var(--btc-ink)',
          900: 'var(--btc-ink)',
        },
        rose: {
          50: 'var(--btc-error-bg)',
          100: 'var(--btc-error-bg)',
          200: 'var(--btc-error-border)',
          600: 'var(--btc-error-text)',
          700: 'var(--btc-error-text)',
        },
        amber: {
          50: 'var(--btc-warning-bg)',
          100: 'var(--btc-warning-bg)',
          200: 'var(--btc-warning-border)',
          300: 'var(--btc-warning-border)',
          500: 'var(--btc-warning-text)',
          700: 'var(--btc-warning-text)',
          800: 'var(--btc-warning-text)',
        },
        blue: {
          50: 'var(--btc-info-bg)',
          100: 'var(--btc-info-bg)',
          200: 'var(--btc-info-border)',
          600: 'var(--btc-info-text)',
          700: 'var(--btc-info-text)',
          800: 'var(--btc-info-text)',
          900: 'var(--btc-info-text)',
        },
        cyan: {
          50: 'var(--btc-info-bg)',
          300: 'var(--btc-info-border)',
          700: 'var(--btc-info-text)',
        },
        sky: {
          50: 'var(--btc-info-bg)',
          100: 'var(--btc-info-bg)',
          200: 'var(--btc-info-border)',
        },
        emerald: {
          100: 'var(--btc-success-bg)',
          700: 'var(--btc-success-text)',
        }
      }
    },
  },
  plugins: [],
}
