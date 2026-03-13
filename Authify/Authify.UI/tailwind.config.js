/** @type {import('tailwindcss').Config} */
module.exports = {
    // Dark mode = default (:root), light mode = [data-theme="light"]
    // Matches SimpliAI Host configuration
    darkMode: ['selector', ':root:not([data-theme="light"])'],
    prefix: 'auth-',
    content: [
        './**/*.{razor,html,cs}',
    ],
    theme: {
        extend: {
            fontFamily: {
                display: ['Syne', 'sans-serif'],
                sans:    ['DM Sans', 'sans-serif'],
            },
            animation: {
                'float':      'float 8s ease-in-out infinite',
                'fade-up':    'fadeUp 0.7s cubic-bezier(0.16, 1, 0.3, 1) forwards',
                'fade-in':    'fadeIn 0.5s ease forwards',
                'fade-in-down': 'fadeInDown 0.3s ease-out forwards',
            },
            keyframes: {
                float: {
                    '0%, 100%': { transform: 'translateY(0) rotate(0deg)' },
                    '50%':      { transform: 'translateY(-20px) rotate(180deg)' },
                },
                fadeUp: {
                    from: { opacity: '0', transform: 'translateY(32px)' },
                    to:   { opacity: '1', transform: 'translateY(0)' },
                },
                fadeIn: {
                    from: { opacity: '0' },
                    to:   { opacity: '1' },
                },
                fadeInDown: {
                    '0%':   { opacity: '0', transform: 'translateY(-10px)' },
                    '100%': { opacity: '1', transform: 'translateY(0)' },
                }
            },
            transitionTimingFunction: {
                'expo-out': 'cubic-bezier(0.16, 1, 0.3, 1)',
            },
        },
    },
    corePlugins: {
        preflight: false,
    },
    plugins: [],
}