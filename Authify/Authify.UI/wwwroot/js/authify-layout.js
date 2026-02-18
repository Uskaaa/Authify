// Authify Layout JavaScript helpers
window.authify = window.authify || {};

/**
 * Toggle mobile menu visibility
 * @param {boolean} isOpen - Whether the menu should be open
 */
window.authify.toggleMobileMenu = function(isOpen) {
    const sidebar = document.getElementById('profile-mobile-sidebar');
    const backdrop = document.getElementById('profile-mobile-backdrop');
    
    if (!sidebar || !backdrop) {
        console.warn('Authify: Mobile menu elements not found');
        return;
    }
    
    if (isOpen) {
        sidebar.classList.remove('-auth-translate-x-full');
        sidebar.classList.add('auth-translate-x-0');
        backdrop.classList.remove('auth-hidden');
        backdrop.classList.add('auth-block');
    } else {
        sidebar.classList.add('-auth-translate-x-full');
        sidebar.classList.remove('auth-translate-x-0');
        backdrop.classList.add('auth-hidden');
        backdrop.classList.remove('auth-block');
    }
};
