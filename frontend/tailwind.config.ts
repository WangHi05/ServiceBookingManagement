import type { Config } from 'tailwindcss';

const config: Config = {
  content: [
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        stone: {
          50: '#F6F7F4',
          100: '#EDEEE8',
          200: '#DCDED7',
        },
        ink: {
          DEFAULT: '#1F2A24',
          light: '#4B564F',
        },
        sage: {
          50: '#EEF3EE',
          100: '#D6E3D8',
          300: '#8FAF97',
          500: '#3F6C51',
          600: '#345A43',
          700: '#294636',
        },
        amber: {
          400: '#C99A4E',
          500: '#B8863B',
          600: '#9C6F2E',
        },
        rose: {
          500: '#B3261E',
          50: '#FBEAE9',
        },
      },
      fontFamily: {
        display: ['var(--font-fraunces)', 'Georgia', 'serif'],
        sans: ['var(--font-inter)', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
};

export default config;
