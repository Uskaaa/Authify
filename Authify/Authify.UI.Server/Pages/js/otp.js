window.initOtp = () => {
    const otpForm = document.getElementById('otpForm');
    const otpInputs = document.querySelectorAll('.otp-inputs input');
    const timerElement = document.getElementById('timer');
    const resendButton = document.getElementById('resendCode');
    const successMessage = document.querySelector('.success-message');

    let timeLeft = 60; // 60 seconds
    let timerInterval;

    // Start the timer
    function startTimer() {
        timerInterval = setInterval(() => {
            timeLeft--;
            const minutes = Math.floor(timeLeft / 60);
            const seconds = timeLeft % 60;
            timerElement.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;

            if (timeLeft <= 0) {
                clearInterval(timerInterval);
                resendButton.style.display = 'inline';
                document.querySelector('.otp-timer').style.display = 'none';
            }
        }, 1000);
    }

    // Handle OTP input
    otpInputs.forEach((input, index) => {
        // Handle input
        input.addEventListener('input', (e) => {
            const value = e.target.value;

            // Only allow numbers
            if (!/^\d*$/.test(value)) {
                e.target.value = '';
                return;
            }

            // Move to next input if value is entered
            if (value && index < otpInputs.length - 1) {
                otpInputs[index + 1].focus();
            }
        });

        // Handle backspace
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Backspace' && !e.target.value && index > 0) {
                otpInputs[index - 1].focus();
            }
        });

        // Handle paste
        input.addEventListener('paste', (e) => {
            e.preventDefault();
            const pastedData = e.clipboardData.getData('text').slice(0, 6);

            if (/^\d+$/.test(pastedData)) {
                pastedData.split('').forEach((digit, i) => {
                    if (otpInputs[i]) {
                        otpInputs[i].value = digit;
                    }
                });

                // Focus the last input or the next empty input
                const lastInput = otpInputs[pastedData.length - 1] || otpInputs[otpInputs.length - 1];
                lastInput.focus();
            }
        });
    });

    // Form submission
    otpForm.addEventListener('submit', (e) => {
        e.preventDefault();

        const otp = Array.from(otpInputs).map(input => input.value).join('');

        if (otp.length !== 6) {
            return;
        }

        // Here you would typically verify the OTP with your server
        console.log('OTP submitted:', otp);

        // Show loading state
        const submitButton = otpForm.querySelector('button[type="submit"]');
        submitButton.disabled = true;
        submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Verifying...';

        // Simulate API call
        setTimeout(() => {
            // Hide form and show success message
            otpForm.style.display = 'none';
            successMessage.style.display = 'block';

            // Reset button state
            submitButton.disabled = false;
            submitButton.textContent = 'Verify';
        }, 2000);
    });

    // Resend code
    resendButton.addEventListener('click', (e) => {
        e.preventDefault();

        // Reset timer
        timeLeft = 60;
        startTimer();

        // Hide resend button and show timer
        resendButton.style.display = 'none';
        document.querySelector('.otp-timer').style.display = 'flex';

        // Clear OTP inputs
        otpInputs.forEach(input => {
            input.value = '';
        });
        otpInputs[0].focus();

        // Here you would typically request a new OTP from your server
        console.log('Requesting new OTP');
    });

    // Start initial timer
    startTimer();
}; 