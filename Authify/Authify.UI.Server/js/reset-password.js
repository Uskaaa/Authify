window.initResetPassword = () => {
    const resetPasswordForm = document.getElementById('resetPasswordForm');
    const passwordInput = document.getElementById('password');
    const confirmPasswordInput = document.getElementById('confirmPassword');
    const togglePasswordBtns = document.querySelectorAll('.toggle-password');
    const successMessage = document.querySelector('.success-message');

    // Toggle password visibility
    togglePasswordBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const input = btn.closest('.password-input').querySelector('input');
            const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
            input.setAttribute('type', type);
            btn.querySelector('i').classList.toggle('fa-eye');
            btn.querySelector('i').classList.toggle('fa-eye-slash');
        });
    });

    // Form validation
    const validatePassword = (password) => {
        return password.length >= 6;
    };

    const validateConfirmPassword = (password, confirmPassword) => {
        return password === confirmPassword;
    };

    const showError = (input, message) => {
        const formGroup = input.closest('.form-group');
        const errorElement = formGroup.querySelector('.error-message');
        errorElement.textContent = message;
        input.classList.add('error');
    };

    const clearError = (input) => {
        const formGroup = input.closest('.form-group');
        const errorElement = formGroup.querySelector('.error-message');
        errorElement.textContent = '';
        input.classList.remove('error');
    };

    // Real-time validation
    passwordInput.addEventListener('input', () => {
        if (!validatePassword(passwordInput.value)) {
            showError(passwordInput, 'Password must be at least 6 characters');
        } else {
            clearError(passwordInput);
        }

        // Update confirm password validation
        if (confirmPasswordInput.value) {
            if (!validateConfirmPassword(passwordInput.value, confirmPasswordInput.value)) {
                showError(confirmPasswordInput, 'Passwords do not match');
            } else {
                clearError(confirmPasswordInput);
            }
        }
    });

    confirmPasswordInput.addEventListener('input', () => {
        if (!validateConfirmPassword(passwordInput.value, confirmPasswordInput.value)) {
            showError(confirmPasswordInput, 'Passwords do not match');
        } else {
            clearError(confirmPasswordInput);
        }
    });

    // Form submission
    resetPasswordForm.addEventListener('submit', (e) => {
        e.preventDefault();

        let isValid = true;

        // Validate password
        if (!validatePassword(passwordInput.value)) {
            showError(passwordInput, 'Password must be at least 6 characters');
            isValid = false;
        }

        // Validate confirm password
        if (!validateConfirmPassword(passwordInput.value, confirmPasswordInput.value)) {
            showError(confirmPasswordInput, 'Passwords do not match');
            isValid = false;
        }

        if (isValid) {
            // Here you would typically send the new password to your server
            console.log('Password reset:', {
                password: passwordInput.value
            });

            // Show loading state
            const submitButton = resetPasswordForm.querySelector('button[type="submit"]');
            submitButton.disabled = true;
            submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Resetting...';

            // Simulate API call
            setTimeout(() => {
                // Hide form and show success message
                resetPasswordForm.style.display = 'none';
                successMessage.style.display = 'block';

                // Reset button state
                submitButton.disabled = false;
                submitButton.textContent = 'Reset Password';
            }, 2000);
        }
    });
}; 