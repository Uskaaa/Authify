window.initRegister = () => {
    const registerForm = document.getElementById('registerForm');
    const fullNameInput = document.getElementById('fullName');
    const emailInput = document.getElementById('email');
    const passwordInput = document.getElementById('password');
    const confirmPasswordInput = document.getElementById('confirmPassword');
    const togglePasswordBtns = document.querySelectorAll('.toggle-password');

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
    const validateFullName = (name) => {
        return name.length >= 2;
    };

    const validateEmail = (email) => {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
    };

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
    fullNameInput.addEventListener('input', () => {
        if (!validateFullName(fullNameInput.value)) {
            showError(fullNameInput, 'Please enter your full name');
        } else {
            clearError(fullNameInput);
        }
    });

    emailInput.addEventListener('input', () => {
        if (!validateEmail(emailInput.value)) {
            showError(emailInput, 'Please enter a valid email address');
        } else {
            clearError(emailInput);
        }
    });

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
    registerForm.addEventListener('submit', (e) => {
        e.preventDefault();

        let isValid = true;

        // Validate full name
        if (!validateFullName(fullNameInput.value)) {
            showError(fullNameInput, 'Please enter your full name');
            isValid = false;
        }

        // Validate email
        if (!validateEmail(emailInput.value)) {
            showError(emailInput, 'Please enter a valid email address');
            isValid = false;
        }

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

        // Validate terms checkbox
        const termsCheckbox = document.querySelector('input[name="terms"]');
        if (!termsCheckbox.checked) {
            showError(termsCheckbox, 'You must agree to the terms and conditions');
            isValid = false;
        }

        if (isValid) {
            // Here you would typically send the form data to your server
            console.log('Form submitted:', {
                fullName: fullNameInput.value,
                email: emailInput.value,
                password: passwordInput.value
            });

            // Simulate successful registration
            const submitButton = registerForm.querySelector('button[type="submit"]');
            submitButton.disabled = true;
            submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Creating account...';

            setTimeout(() => {
                submitButton.disabled = false;
                submitButton.textContent = 'Create Account';
                // Redirect to login page
                window.location.href = 'login.html';
            }, 2000);
        }
    });

    // OAuth button handlers
    document.querySelector('.btn-oauth.google').addEventListener('click', () => {
        console.log('Google OAuth clicked');
        // Implement Google OAuth logic here
    });

    document.querySelector('.btn-oauth.github').addEventListener('click', () => {
        console.log('GitHub OAuth clicked');
        // Implement GitHub OAuth logic here
    });
}; 