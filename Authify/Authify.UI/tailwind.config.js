/** @type {import('tailwindcss').Config} */
module.exports = {
    prefix: 'auth-',           // kannst du behalten (wenn du Isolation brauchst)
    darkMode: ['class', '[class~="dark"]'],        // ← wichtig: genau wie im Host
    content: ['./**/*.{razor,html,cs}'],
    theme: {
        extend: {
            fontFamily: {
                sans: ['Inter', 'system-ui', 'sans-serif'],
            },
            colors: {
                primary: {
                    50: '#eef2ff',
                    100: '#e0e7ff',
                    200: '#c7d2fe',
                    300: '#a5b4fc',
                    400: '#818cf8',
                    500: '#6366f1',
                    600: '#4f46e5',
                    700: '#4338ca',
                    800: '#3730a3',
                    900: '#312e81',
                    950: '#1e1b4b',
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