window.initForgotPassword = () => {
    const forgotPasswordForm = document.getElementById('forgotPasswordForm');
    const emailInput = document.getElementById('email');
    const successMessage = document.querySelector('.success-message');

    // Form validation
    const validateEmail = (email) => {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
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
    emailInput.addEventListener('input', () => {
        if (!validateEmail(emailInput.value)) {
            showError(emailInput, 'Please enter a valid email address');
        } else {
            clearError(emailInput);
        }
    });

    // Form submission
    forgotPasswordForm.addEventListener('submit', (e) => {
        e.preventDefault();

        if (!validateEmail(emailInput.value)) {
            showError(emailInput, 'Please enter a valid email address');
            return;
        }

        // Here you would typically send the reset request to your server
        console.log('Reset request for:', emailInput.value);

        // Show loading state
        const submitButton = forgotPasswordForm.querySelector('button[type="submit"]');
        submitButton.disabled = true;
        submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Sending...';

        // Simulate API call
        setTimeout(() => {
            // Hide form and show success message
            forgotPasswordForm.style.display = 'none';
            successMessage.style.display = 'block';

            // Reset button state
            submitButton.disabled = false;
            submitButton.textContent = 'Send Reset Link';
        }, 2000);
    });
}; 