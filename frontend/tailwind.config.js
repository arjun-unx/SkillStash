/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'monospace']
      },
      colors: {
        surface: {
          DEFAULT: '#0a0c10',
          elevated: '#12151c',
          raised: '#181c26',
          overlay: '#1e2430'
        },
        accent: {
          DEFAULT: '#7c5cff',
          hover: '#9178ff',
          muted: '#b8a7ff'
        },
        line: {
          DEFAULT: '#252b38',
          strong: '#343c4d'
        }
      },
      boxShadow: {
        card: '0 1px 0 rgba(255,255,255,0.04) inset, 0 20px 50px -24px rgba(0,0,0,0.65)',
        glow: '0 0 48px -12px rgba(124, 92, 255, 0.45)'
      },
      backgroundImage: {
        'mesh-auth':
          'radial-gradient(ellipse 80% 60% at 10% 0%, rgba(124,92,255,0.22), transparent 55%), radial-gradient(ellipse 60% 50% at 90% 100%, rgba(56,189,248,0.14), transparent 50%)',
        'mesh-app':
          'radial-gradient(ellipse 70% 50% at 100% 0%, rgba(124,92,255,0.12), transparent 50%), radial-gradient(ellipse 50% 40% at 0% 100%, rgba(56,189,248,0.08), transparent 45%)'
      }
    }
  },
  plugins: [require('@tailwindcss/forms'), require('@tailwindcss/typography')]
};
