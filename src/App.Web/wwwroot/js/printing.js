window.createPdfBlobUrl = function(base64Data) {
    const byteCharacters = atob(base64Data);
    const byteNumbers = new Uint8Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const blob = new Blob([byteNumbers], { type: 'application/pdf' });
    return URL.createObjectURL(blob);
};

window.revokeBlobUrl = function(url) {
    if (url) URL.revokeObjectURL(url);
};

window.printTicket = async function(url) {
    // Verificar que la URL es válida
    if (!url || !url.startsWith('/')) {
        console.error("URL de ticket inválida:", url);
        throw new Error("URL de ticket inválida");
    }
    
    // Asegurar que la URL sea absoluta
    const baseUrl = window.location.origin;
    const fullUrl = url.startsWith('http') ? url : `${baseUrl}${url}`;
    
    console.log("Imprimiendo ticket:", fullUrl);
    
    try {
        // Crear ventana de impresión con tamaño adecuado para tickets
        const printWindow = window.open(fullUrl, 'TicketPrint', 'width=600,height=600,toolbar=0,menubar=0');
        
        if (!printWindow) {
            throw new Error("No se pudo abrir la ventana de impresión. Verifique que no esté bloqueado por el navegador.");
        }
        
        // Esperar a que se cargue el contenido
        await new Promise((resolve, reject) => {
            let checkReadyInterval;
            let loadTimeoutId;
            
            // Función para limpiar todos los temporizadores
            const clearAllTimers = () => {
                if (checkReadyInterval) clearInterval(checkReadyInterval);
                if (loadTimeoutId) clearTimeout(loadTimeoutId);
            };
            
            // Función para verificar si el documento está listo
            const checkIfReady = () => {
                try {
                    // Si podemos acceder al documento y está en estado 'complete', está listo
                    if (printWindow.document && printWindow.document.readyState === 'complete') {
                        console.log("Documento cargado completamente");
                        clearAllTimers(); // Limpiar todos los temporizadores
                        
                        // Dar tiempo adicional para que el PDF se renderice completamente
                        setTimeout(() => {
                            try {
                                // Imprimir el documento
                                printWindow.focus();
                                printWindow.print();
                                console.log("Impresión iniciada");
                                                                
                                resolve();
                            } catch (err) {
                                console.error("Error durante la impresión:", err);
                                reject(err);
                            }
                        }, 1000);
                    }
                } catch (err) {
                    clearAllTimers();
                    reject(err);
                }
            };
            
            // Verificar periódicamente si el documento está listo
            checkReadyInterval = setInterval(checkIfReady, 200);
            
            // También registrar el evento load como respaldo
            printWindow.addEventListener('load', () => {
                console.log("Evento load disparado");
                checkIfReady(); // Verificar inmediatamente después del evento load
            });
            
            // Timeout por si la carga falla completamente
            loadTimeoutId = setTimeout(() => {
                clearAllTimers();
                reject(new Error("Tiempo de espera agotado al cargar la página de impresión"));
            }, 20000);
        });
        
        return true;
    } catch (err) {
        console.error("Error durante la impresión:", err);
        throw err;
    }
};