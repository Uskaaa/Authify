class OtpInputHandler {
    constructor(containerElement, dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        this.container = containerElement;
        this.inputs = Array.from(containerElement.querySelectorAll('input.otp-input'));
        this.inputs[0].focus();
        this.hiddenInput = containerElement.querySelector('input[type="hidden"]');
        this.attachEvents();
    }

    attachEvents() {
        this.inputs.forEach((input, index) => {
            input.addEventListener('input', (e) => this.handleInput(e, index));
            input.addEventListener('keydown', (e) => this.handleKeyDown(e, index));
            input.addEventListener('focus', (e) => this.handleFocus(e));
            input.addEventListener('paste', (e) => this.handlePaste(e, index));
        });
    }

    handleInput(e, index) {
        const input = e.target;
        const value = input.value;

        // Nur eine Ziffer pro Feld erlauben
        if (value.length > 1) {
            input.value = value[0];
        }

        if (input.value && index < this.inputs.length - 1) {
            this.inputs[index + 1].focus();
        }

        this.gatherOtpAndNotifyBlazor();
    }

    handleKeyDown(e, index) {
        if (e.key === 'Backspace' && !e.target.value && index > 0) {
            this.inputs[index - 1].focus();
        }
    }

    handleFocus(e) {
        e.target.select();
    }

    handlePaste(e, index) {
        e.preventDefault();
        const pastedData = e.clipboardData.getData('text').trim();

        if (!/^\d+$/.test(pastedData)) {
            return; // Nur Ziffern einfügen
        }

        for (let i = 0; i < pastedData.length; i++) {
            if (index + i < this.inputs.length) {
                this.inputs[index + i].value = pastedData[i];
            }
        }

        const nextFocusIndex = Math.min(index + pastedData.length, this.inputs.length - 1);
        this.inputs[nextFocusIndex].focus();
        this.gatherOtpAndNotifyBlazor();
    }

    gatherOtpAndNotifyBlazor() {
        const otp = this.inputs.map(input => input.value).join('');

        // Verstecktes Input-Feld für Blazor @bind aktualisieren
        this.hiddenInput.value = otp;
        // Event manuell auslösen, damit Blazor @bind reagiert
        this.hiddenInput.dispatchEvent(new Event('input', { bubbles: true }));

        // Optional: .NET-Methode direkt aufrufen, wenn OTP vollständig ist
        if (otp.length === this.inputs.length) {
            this.dotNetHelper.invokeMethodAsync('OnOtpCompleted', otp);
        }
    }
}

// Diese Funktion wird von Blazor aufgerufen, um alles zu initialisieren
window.initializeOtpHandler = (containerElement, dotNetHelper) => {
    new OtpInputHandler(containerElement, dotNetHelper);
};