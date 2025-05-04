window.initLogin = () => {
    const loginForm = document.getElementById('loginForm');
    const emailInput = document.getElementById('email');
    const passwordInput = document.getElementById('password');
    const togglePasswordBtn = document.querySelector('.toggle-password');
    const submitButton = loginForm.querySelector('button[type="submit"]');
    const googleButton = document.querySelector('.btn-oauth.google');
    const githubButton = document.querySelector('.btn-oauth.github');
    const forgotPasswordBtn = document.getElementById('forgot-password-btn');
    const signUpLinkBtn = document.getElementById('sign-up-link');

    // Toggle password visibility
    togglePasswordBtn.addEventListener('click', () => {
        const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
        passwordInput.setAttribute('type', type);
        togglePasswordBtn.querySelector('i').classList.toggle('fa-eye');
        togglePasswordBtn.querySelector('i').classList.toggle('fa-eye-slash');
    });

    // Form validation
    const validateEmail = (email) => {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
    };

    const validatePassword = (password) => {
        return password.length >= 6;
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

    passwordInput.addEventListener('input', () => {
        if (!validatePassword(passwordInput.value)) {
            showError(passwordInput, 'Password must be at least 6 characters');
        } else {
            clearError(passwordInput);
        }
    });

    // Form submission
    loginForm.addEventListener('submit', (e) => {
        e.preventDefault();

        let isValid = true;

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

        if (isValid) {
            // Here you would typically send the form data to your server
            console.log('Form submitted:', {
                email: emailInput.value,
                password: passwordInput.value,
                remember: document.querySelector('input[name="remember"]').checked
            });
            submitButton.disabled = true;
            googleButton.disabled = true;
            githubButton.disabled = true;
            submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Logging in...';

            setTimeout(() => {
                submitButton.disabled = false;
                googleButton.disabled = false;
                githubButton.disabled = false;
                submitButton.textContent = 'Login';
            }, 2000);
        }
    });

    // OAuth button handlers
    googleButton.addEventListener('click', () => {
        console.log('Google OAuth clicked');
        googleButton.disabled = true;
        githubButton.disabled = true;
        submitButton.disabled = true;
        googleButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Logging in...';

        setTimeout(() => {
            googleButton.disabled = false;
            githubButton.disabled = false;
            submitButton.disabled = false;
            googleButton.innerHTML = '<i class="fab fa-google"></i> <span>Sign in with Google</span>';
        }, 2000);
    });

    githubButton.addEventListener('click', () => {
        console.log('GitHub OAuth clicked');
        githubButton.disabled = true;
        googleButton.disabled = true;
        submitButton.disabled = true;
        githubButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Logging in...';

        setTimeout(() => {
            githubButton.disabled = false;
            googleButton.disabled = false;
            submitButton.disabled = false;
            githubButton.innerHTML = '<i class="fab fa-github"></i> <span>Sign in with GitHub</span>';
        }, 2000);
    });
}; 