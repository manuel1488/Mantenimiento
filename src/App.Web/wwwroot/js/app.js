// POS keyboard shortcuts — F2/F3 focus, F4-F9 payment methods, F12 complete sale
window.posKeyboard = {
    _dotNetRef: null,
    _handler: null,
    _scannerHandler: null,
    _lastKeyTime: 0,
    register: function (dotNetRef) {
        this._dotNetRef = dotNetRef;

        // F-key shortcuts (bubble phase)
        this._handler = async (e) => {
            const handled = ['F2', 'F3', 'F4', 'F5', 'F6', 'F7', 'F8', 'F9', 'F12'];
            if (!handled.includes(e.key)) return;
            e.preventDefault();
            await this._dotNetRef.invokeMethodAsync('HandleKeyShortcut', e.key);
        };
        document.addEventListener('keydown', this._handler);

        // Scanner Enter detection (capture phase — fires before MudAutocomplete)
        const self = this;
        this._scannerHandler = (e) => {
            const input = document.querySelector('.product-search-autocomplete input');
            if (!input || document.activeElement !== input) return;

            if (e.key !== 'Enter') {
                self._lastKeyTime = Date.now();
                return;
            }

            const elapsed = Date.now() - self._lastKeyTime;
            const text = input.value;
            if (elapsed < 150 && text) {
                // Fast input = barcode scanner: intercept Enter and handle directly
                e.stopImmediatePropagation();
                e.preventDefault();
                if (self._dotNetRef) {
                    self._dotNetRef.invokeMethodAsync('HandleScannerEnter', text);
                }
            }
        };
        document.addEventListener('keydown', this._scannerHandler, true);
    },
    unregister: function () {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
            this._handler = null;
        }
        if (this._scannerHandler) {
            document.removeEventListener('keydown', this._scannerHandler, true);
            this._scannerHandler = null;
        }
        if (this._dotNetRef) {
            this._dotNetRef.dispose();
            this._dotNetRef = null;
        }
    }
};

// Open a URL in a new browser tab — called from Blazor via JS interop
window.openInNewTab = (url) => {
    window.open(url, '_blank', 'noopener,noreferrer');
};

// Open base64 content in a new browser tab
window.openFileInNewTab = (contentType, base64Data) => {
    const byteCharacters = atob(base64Data);
    const byteNumbers = new Uint8Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const blob = new Blob([byteNumbers], { type: contentType });
    const url = URL.createObjectURL(blob);
    window.open(url, '_blank', 'noopener,noreferrer');
};

// File download helper — called from Blazor via JS interop
window.downloadFile = (fileName, contentType, base64Data) => {
    const link = document.createElement('a');
    link.href = `data:${contentType};base64,${base64Data}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Configuración mejorada de reconexión de Blazor
Blazor.start({
    circuit: {
        reconnectionOptions: {
            maxRetries: 15,
            retryIntervalMilliseconds: 1000,
            dialogId: 'components-reconnect-modal'
        }
    }
});

// Personalizar el UI de reconexión para hacerlo menos intrusivo
window.addEventListener('load', () => {
    const reconnectModal = document.getElementById('components-reconnect-modal');
    if (reconnectModal) {
        // Hacer el modal menos intrusivo - notificación tipo toast
        reconnectModal.style.cssText = `
            position: fixed !important;
            top: 20px !important;
            right: 20px !important;
            left: auto !important;
            bottom: auto !important;
            background: rgba(255, 152, 0, 0.95) !important;
            color: white !important;
            padding: 12px 20px !important;
            border-radius: 8px !important;
            font-size: 14px !important;
            box-shadow: 0 4px 12px rgba(0,0,0,0.2) !important;
            z-index: 9999 !important;
            max-width: 320px !important;
            transform: none !important;
        `;
    }
});