/** @type {import('tailwindcss').Config} */
module.exports = {
    prefix: 'auth-',
    darkMode: ['selector', ':root:not([data-theme="light"])'],
    content: ['./**/*.{razor,html,cs}'],
    theme: {
        extend: {
            fontFamily: {
                sans: ['Inter', 'system-ui', 'sans-serif'],
            },
            colors: {
                // Primary palette – resolved at runtime via CSS custom properties.
                // Each shade is defined as an RGB triplet variable so that Tailwind's
                // opacity modifier syntax (e.g. auth-bg-primary-600/20) continues to work.
                primary: {
                    50:  'rgb(var(--auth-primary-50-rgb)  / <alpha-value>)',
                    100: 'rgb(var(--auth-primary-100-rgb) / <alpha-value>)',
                    200: 'rgb(var(--auth-primary-200-rgb) / <alpha-value>)',
                    300: 'rgb(var(--auth-primary-300-rgb) / <alpha-value>)',
                    400: 'rgb(var(--auth-primary-400-rgb) / <alpha-value>)',
                    500: 'rgb(var(--auth-primary-500-rgb) / <alpha-value>)',
                    600: 'rgb(var(--auth-primary-600-rgb) / <alpha-value>)',
                    700: 'rgb(var(--auth-primary-700-rgb) / <alpha-value>)',
                    800: 'rgb(var(--auth-primary-800-rgb) / <alpha-value>)',
                    900: 'rgb(var(--auth-primary-900-rgb) / <alpha-value>)',
                    950: 'rgb(var(--auth-primary-950-rgb) / <alpha-value>)',
                },

                // ────────────────────────────────────────────────
                //  → Hier die wichtigste Änderung ←
                // Entferne das komplette gray-Override!
                // Füge stattdessen slate hinzu – genau wie im Host
                slate: {
                    50:  '#f8fafc',
                    100: '#f1f5f9',
                    200: '#e2e8f0',
                    300: '#cbd5e1',
                    400: '#94a3b8',
                    500: '#64748b',
                    600: '#475569',
                    700: '#334155',
                    800: '#1e293b',
                    900: '#0f172a',
                    950: '#020617',
                    // Bonus: genau wie Host
                    850: '#151f32',
                },

                // Optional – falls du die dark.{bg,card,…} Semantik behalten willst:
                dark: {
                    bg:    '#020617',    // slate-950
                    card:  '#0f172a',    // slate-900
                    border:'#1e293b',    // slate-800
                    text:  '#f8fafc',    // slate-50
                    muted: '#94a3b8',    // slate-400
                }
            },

            animation: {
                'fade-in-down': 'fadeInDown 0.3s ease-out forwards',
            },
            keyframes: {
                fadeInDown: {
                    '0%':   { opacity: '0', transform: 'translateY(-10px)' },
                    '100%': { opacity: '1', transform: 'translateY(0)'    },
                }
            }
        },
    },
    corePlugins: {
        preflight: false, // wichtig – bleibt
    },
    plugins: [],
}