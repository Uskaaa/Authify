export function init() {
    // --- Password Strength Indicator ---
    // Diese Logik bleibt in JS, da sie direktes DOM-Styling basierend auf Input macht
    class PasswordStrengthIndicator {
        constructor() {
            this.passwordInput = document.getElementById('new-password');
            this.strengthText = document.getElementById('strength-text');
            this.strengthBar = document.getElementById('strength-bar');
            this.lengthCheck = document.getElementById('length-check');
            this.uppercaseCheck = document.getElementById('uppercase-check');
            this.specialCheck = document.getElementById('special-check');

            if (this.passwordInput) {
                this.init();
            }
        }

        init() {
            this.passwordInput.addEventListener('input', (e) => {
                this.updateStrength(e.target.value);
            });
            // Initialize with empty password
            this.updateStrength(this.passwordInput.value || '');
        }

        updateStrength(password) {
            const hasLength = password.length >= 8;
            const hasUppercase = /[A-Z]/.test(password);
            const hasSpecialChar = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password);

            this.updateCriterion(this.lengthCheck, hasLength);
            this.updateCriterion(this.uppercaseCheck, hasUppercase);
            this.updateCriterion(this.specialCheck, hasSpecialChar);

            const fulfilledCount = [hasLength, hasUppercase, hasSpecialChar].filter(Boolean).length;
            this.updateStrengthDisplay(fulfilledCount, password);
        }

        updateCriterion(element, isFulfilled) {
            if (!element) return;
            const icon = element.querySelector('svg');

            if (isFulfilled) {
                element.classList.remove('text-gray-500', 'dark:text-dark-muted');
                element.classList.add('text-green-600', 'dark:text-green-400');

                if(icon) {
                    icon.classList.remove('text-gray-300', 'dark:text-dark-muted');
                    icon.classList.add('text-green-500');
                    // Checkmark Icon
                    icon.innerHTML = `<path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"></path>`;
                }
            } else {
                element.classList.remove('text-green-600', 'dark:text-green-400');
                element.classList.add('text-gray-500', 'dark:text-dark-muted');

                if(icon) {
                    icon.classList.remove('text-green-500');
                    icon.classList.add('text-gray-300', 'dark:text-dark-muted');
                    // Dot Icon
                    icon.innerHTML = `<path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path>`;
                }
            }
        }

        updateStrengthDisplay(fulfilledCount, password) {
            this.strengthText.className = 'font-medium transition-colors duration-300';
            this.strengthBar.className = 'h-2 rounded-full transition-all duration-300';

            if (password.length === 0) {
                this.strengthText.classList.add('text-gray-500', 'dark:text-dark-muted');
                this.strengthText.textContent = '';
                this.strengthBar.classList.add('bg-gray-200', 'dark:bg-dark-border');
                this.strengthBar.style.width = '0%';
                return;
            }

            switch (fulfilledCount) {
                case 0:
                case 1:
                    this.strengthText.classList.add('text-red-600');
                    this.strengthText.textContent = 'Weak';
                    this.strengthBar.classList.add('bg-red-500');
                    this.strengthBar.style.width = '25%';
                    break;
                case 2:
                    this.strengthText.classList.add('text-orange-600');
                    this.strengthText.textContent = 'Medium';
                    this.strengthBar.classList.add('bg-orange-500');
                    this.strengthBar.style.width = '60%';
                    break;
                case 3:
                    this.strengthText.classList.add('text-green-600');
                    this.strengthText.textContent = 'Strong';
                    this.strengthBar.classList.add('bg-green-500');
                    this.strengthBar.style.width = '100%';
                    break;
            }
        }
    }

    // Nur den Password Indicator instanziieren
    new PasswordStrengthIndicator();
}