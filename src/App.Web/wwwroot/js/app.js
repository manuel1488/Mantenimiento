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