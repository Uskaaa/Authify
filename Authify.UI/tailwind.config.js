/** @type {import('tailwindcss').Config} */
module.exports = {
    prefix: 'auth-',
    darkMode: ['selector', ':root:not([data-theme="light"])'],
    content: ['./**/*.{razor,html,cs}'],
    theme: {
        extend: {
            fontFamily: {
                display: ['Syne', 'sans-serif'],
                sans: ['DM Sans', 'sans-serif'],
            },
            animation: {
                'fade-in-down': 'fadeInDown 0.3s ease-out forwards',
                'fade-in': 'fadeIn 0.5s ease forwards',
            },
            keyframes: {
                fadeInDown: {
                    '0%':   { opacity: '0', transform: 'translateY(-10px)' },
                    '100%': { opacity: '1', transform: 'translateY(0)'    },
                },
                fadeIn: {
                    from: { opacity: '0' },
                    to: { opacity: '1' },
                },
            },
            transitionTimingFunction: {
                'expo-out': 'cubic-bezier(0.16, 1, 0.3, 1)',
            }
        },
    },
    corePlugins: {
        preflight: false, // wichtig – bleibt
    },
    plugins: [],
}
