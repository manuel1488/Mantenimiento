'use strict';

/**
 * webserial-print.js
 * Direct thermal printing via Web Serial API + Epson TM Virtual Port Driver.
 * Works in Chrome/Edge. Requires one-time port permission from the user.
 *
 * Setup on each client terminal:
 *  1. Assign the TM-T20IV USB to a COM port using "Epson TM Virtual Port Assignment Tool"
 *  2. In Admin → Settings → Ticket, click "Connect printer port" and select that COM port
 *  3. Chrome remembers the permission — subsequent prints are automatic
 *
 * Port lifecycle: the port is opened once and kept open between prints.
 * On any hardware error (no-signal, dle-eot-timeout, send-error) the port is
 * closed and nulled so the next print attempt opens it fresh.
 */
window.thermalPrint = {

    // ── Persistent port state ──────────────────────────────────────────────

    /** Kept open between prints — null when closed or after an error. */
    _port: null,

    /**
     * Returns the open port, opening it if necessary.
     * Returns null (with console.warn) if no port is configured or open times out.
     */
    _ensurePortOpen: async function () {
        var t = window.thermalPrint;

        if (t._port !== null) {
            console.log('[thermalPrint] reusing open port');
            return t._port;
        }

        var ports = await navigator.serial.getPorts();
        if (ports.length === 0) {
            console.warn('[thermalPrint] no-port: no serial port has been granted permission');
            return null;
        }

        var port = ports[0];
        console.log('[thermalPrint] opening port...');

        // port.open() can block indefinitely when the device is physically
        // disconnected but the virtual COM port driver keeps the entry alive in
        // Windows. Race against a 4 s timeout so we fall back to PDF instead.
        var openResult = await Promise.race([
            port.open({ baudRate: 9600 }).then(function () { return 'ok'; }),
            new Promise(function (resolve) { setTimeout(function () { resolve('timeout'); }, 4000); })
        ]);

        if (openResult === 'timeout') {
            console.warn('[thermalPrint] open-timeout: port.open() did not resolve in 4 s — virtual driver may be hung or printer USB disconnected');
            return null;
        }

        console.log('[thermalPrint] port opened successfully');
        t._port = port;
        return port;
    },

    /**
     * Closes the port and clears _port.
     * Called after any error so the next print attempt reopens cleanly.
     * @param {string} reason — logged for diagnostics
     */
    _dropPort: async function (reason) {
        var t = window.thermalPrint;
        if (t._port === null) return;
        console.warn('[thermalPrint] dropping port — reason: %s', reason);
        try { await t._port.close(); } catch (e) {
            console.warn('[thermalPrint] port.close() threw while dropping: %s', e.message);
        }
        t._port = null;
    },

    // ── Public API ─────────────────────────────────────────────────────────

    isSupported: function () {
        return 'serial' in navigator;
    },

    /** Returns {success, description} — must be triggered by a user click */
    requestPort: async function () {
        try {
            const port = await navigator.serial.requestPort();
            const info = port.getInfo();
            const desc = info.usbVendorId
                ? 'USB Device (' + info.usbVendorId.toString(16) + ':' + (info.usbProductId || 0).toString(16) + ')'
                : 'Serial port';
            return { success: true, description: desc };
        } catch (e) {
            return { success: false, description: '' };
        }
    },

    /** Returns true if at least one port has already been granted */
    hasPort: async function () {
        try {
            const ports = await navigator.serial.getPorts();
            return ports.length > 0;
        } catch { return false; }
    },

    /**
     * Print functions return {success, bytesSent, paperStatus, error}.
     * paperStatus: 'ok' | 'near-end' | 'empty' | null (null = could not read).
     */
    printSale: async function (data, flushDelayMs, chunkSize, settlingDelayMs) {
        return window.thermalPrint._send(await window.thermalPrint._buildSale(data), flushDelayMs, chunkSize, true, settlingDelayMs);
    },

    printWithdrawal: async function (data, flushDelayMs, chunkSize, settlingDelayMs) {
        return window.thermalPrint._send(window.thermalPrint._buildWithdrawal(data), flushDelayMs, chunkSize, true, settlingDelayMs);
    },

    printTest: async function (settlingDelayMs) {
        return window.thermalPrint._send(window.thermalPrint._buildTest(), 0, 0, true, settlingDelayMs);
    },

    /**
     * Opens the cash drawer by sending an ESC/POS command via the serial port.
     * commandHex: space-separated hex bytes, e.g. "1B 70 00 19 FA"
     * Returns boolean (no print confirmation needed).
     */
    openDrawer: async function (commandHex, flushDelayMs) {
        try {
            const bytes = (commandHex || '').trim().split(/\s+/).map(function (h) {
                return parseInt(h, 16);
            }).filter(function (b) { return !isNaN(b); });
            if (bytes.length === 0) return false;
            var result = await window.thermalPrint._send(bytes, flushDelayMs || 200);
            return result.success;
        } catch (e) {
            console.error('[thermalPrint] openDrawer error:', e);
            return false;
        }
    },

    // ── Serial port send ───────────────────────────────────────────────────

    /**
     * Core send function. Uses the persistent _port — opens it if needed,
     * drops it on any hardware error so the next call reopens cleanly.
     *
     * @param {number[]} bytes          ESC/POS payload
     * @param {number}   safetyBufferMs Timeout for GS r read / fallback delay (default 2000)
     * @param {number}   chunkSize      Max bytes per write (default 2048)
     * @param {boolean}  confirmPrint   Append GS r 1 and wait for completion response
     * @returns {{success:boolean, bytesSent:number, paperStatus:string|null, error:string|null}}
     */
    _send: async function (bytes, safetyBufferMs, chunkSize, confirmPrint, settlingDelayMs) {
        var t = window.thermalPrint;
        var sent = 0;
        // Diagnostics — hoisted so every return path can include them
        var portFresh = t._port === null;
        var dsr = null, cts = null;
        try {
            var port = await t._ensurePortOpen();
            if (port === null) {
                // Distinguish no-port from open-timeout (_ensurePortOpen already logged)
                var ports = await navigator.serial.getPorts();
                return { success: false, bytesSent: 0, paperStatus: null, error: ports.length === 0 ? 'no-port' : 'open-timeout', portFresh: portFresh, dsr: dsr, cts: cts };
            }

            // ── Virtual COM port settling delay ───────────────────────────
            // After port.open(), the Epson TM Virtual Port Driver asserts
            // DTR/RTS and undergoes a brief driver-USB init cycle. Any bytes
            // written before it completes are silently discarded by the driver
            // — causing the first ticket to be truncated. Must come BEFORE
            // the DLE EOT health check or that check also fails on first print.
            if (portFresh) {
                var delay = (settlingDelayMs != null && settlingDelayMs >= 0) ? settlingDelayMs : 250;
                console.log('[thermalPrint] fresh port — settling delay %d ms', delay);
                await new Promise(function (r) { setTimeout(r, delay); });
            }

            // ── Hardware signal check ──────────────────────────────────────
            // DSR (Data Set Ready) or CTS (Clear To Send) being HIGH means the
            // printer is powered and connected via USB. Both LOW = powered off
            // or USB cable unplugged.
            try {
                var signals = await port.getSignals();
                dsr = signals.dataSetReady;
                cts = signals.clearToSend;
                if (!dsr && !cts) {
                    console.warn('[thermalPrint] no-signal: DSR=%s CTS=%s — printer powered off or USB disconnected', dsr, cts);
                    await t._dropPort('no-signal');
                    return { success: false, bytesSent: 0, paperStatus: null, error: 'no-signal', portFresh: portFresh, dsr: dsr, cts: cts };
                }
                console.log('[thermalPrint] signals ok — DSR=%s CTS=%s', dsr, cts);
            } catch (e) {
                console.log('[thermalPrint] getSignals() not available (%s) — skipping signal check', e.message);
            }

            // ── DLE EOT health check ───────────────────────────────────────
            // Real-time command that bypasses the receive buffer — detects a
            // powered-off printer faster than a full write attempt.
            try {
                var probeOk = await t._dleEot(port);
                if (!probeOk) {
                    console.warn('[thermalPrint] dle-eot-timeout: printer did not respond to DLE EOT within 1.5 s — may be in error state or paper jam');
                    await t._dropPort('dle-eot-timeout');
                    return { success: false, bytesSent: 0, paperStatus: null, error: 'dle-eot-timeout', portFresh: portFresh, dsr: dsr, cts: cts };
                }
            } catch (e) {
                console.log('[thermalPrint] DLE EOT threw (%s) — skipping health check', e.message);
            }

            // ── Build payload ──────────────────────────────────────────────
            // Append GS r n=1 (paper sensor status) after the payload when
            // confirmPrint is requested. GS r is processed FIFO — the 1-byte
            // response only arrives after the printer finishes the cut.
            var payload;
            if (confirmPrint) {
                payload = new Uint8Array(bytes.length + 3);
                payload.set(new Uint8Array(bytes));
                payload.set([0x1D, 0x72, 0x01], bytes.length); // GS r n=1
            } else {
                payload = new Uint8Array(bytes);
            }

            // ── Write in chunks ────────────────────────────────────────────
            // Stay within the printer's 4 KB receive buffer.
            var chunk = chunkSize || 2048;
            var writer = port.writable.getWriter();

            for (var off = 0; off < payload.length; off += chunk) {
                await writer.write(payload.subarray(off, Math.min(off + chunk, payload.length)));
                await writer.ready;
            }
            sent = payload.length;

            // releaseLock() (not close()) keeps the writable stream alive so
            // the port stays open for the next print without a full reopen cycle.
            // writer.ready in the loop above ensures all bytes are flushed to
            // the OS driver buffer before the lock is released.
            writer.releaseLock();

            // ── Read GS r response ─────────────────────────────────────────
            var paperStatus = null;
            var timeoutMs = safetyBufferMs || 2000;

            if (confirmPrint) {
                try {
                    paperStatus = await t._readPaperStatus(port, timeoutMs);
                    console.log('[thermalPrint] print confirmed — %d bytes sent (%d-byte chunks), paper: %s',
                        sent, chunk, paperStatus ?? 'unknown (read timed out)');
                } catch (e) {
                    console.warn('[thermalPrint] GS r read failed (%s) — falling back to %d ms delay', e.message, timeoutMs);
                    await new Promise(function (r) { setTimeout(r, timeoutMs); });
                }
            } else {
                // No confirmation (e.g. cash drawer) — simple delay
                if (timeoutMs > 0) await new Promise(function (r) { setTimeout(r, timeoutMs); });
            }

            return { success: true, bytesSent: sent, paperStatus: paperStatus, error: null, portFresh: portFresh, dsr: dsr, cts: cts };

        } catch (e) {
            console.error('[thermalPrint] send error — %s bytes sent before failure. Error: %s', sent, e.message, e);
            await t._dropPort('send-error: ' + e.message);
            return { success: false, bytesSent: sent, paperStatus: null, error: e.message || 'send-error', portFresh: portFresh, dsr: dsr, cts: cts };
        }
    },

    /**
     * Reads the 1-byte response to GS r n=1 (paper sensor status).
     *   Bits 2-3: 00 = paper OK,  0C = paper near end
     *   Bits 5-6: 00 = paper present, 60 = paper not present
     * Returns 'ok' | 'near-end' | 'empty' | null (timeout).
     */
    _readPaperStatus: async function (port, timeoutMs) {
        var reader = port.readable.getReader();
        try {
            var result = await Promise.race([
                reader.read(),
                new Promise(function (resolve) {
                    setTimeout(function () { resolve({ value: null, done: true }); }, timeoutMs);
                })
            ]);

            if (!result.value || result.value.length === 0) {
                console.warn('[thermalPrint] GS r: no response within %d ms', timeoutMs);
                return null;
            }

            var status = result.value[0];
            var paperEmpty   = (status & 0x60) !== 0;
            var paperNearEnd = (status & 0x0C) !== 0;

            console.log('[thermalPrint] GS r status: 0x%s (empty=%s, nearEnd=%s)',
                status.toString(16).padStart(2, '0'), paperEmpty, paperNearEnd);

            if (paperEmpty)   return 'empty';
            if (paperNearEnd) return 'near-end';
            return 'ok';
        } finally {
            try { reader.cancel(); } catch (_) {}
            reader.releaseLock();
        }
    },

    // ── DLE EOT — real-time printer health check ────────────────────────────

    /**
     * Sends DLE EOT n=1 (0x10 0x04 0x01) and reads the 1-byte status response.
     * This is a real-time command — it bypasses the printer's receive buffer
     * and is processed immediately even if the printer is offline.
     *
     * Response byte (n=1, printer status):
     *   Bit 2: 0 = drawer closed,  1 = drawer open
     *   Bit 3: 0 = online,         1 = waiting for online recovery
     *   Bits 0,1,4-7: fixed values
     *
     * Returns true if the printer responded, false on timeout.
     */
    _dleEot: async function (port) {
        const writer = port.writable.getWriter();
        await writer.write(new Uint8Array([0x10, 0x04, 0x01]));
        await writer.ready;
        writer.releaseLock();

        const reader = port.readable.getReader();
        try {
            const result = await Promise.race([
                reader.read(),
                new Promise(function (resolve) {
                    setTimeout(function () { resolve({ value: null, done: true }); }, 1500);
                })
            ]);

            if (result.value && result.value.length > 0) {
                var status = result.value[0];
                console.log('[thermalPrint] DLE EOT status: 0x%s (drawer %s, online recovery %s)',
                    status.toString(16).padStart(2, '0'),
                    (status & 0x04) ? 'open' : 'closed',
                    (status & 0x08) ? 'waiting' : 'ok');
                return true;
            }
            return false;
        } finally {
            try { reader.cancel(); } catch (_) {}
            reader.releaseLock();
        }
    },

    // ── ESC/POS byte builders ──────────────────────────────────────────────

    _b: {
        init:    [0x1B, 0x40],          // ESC @ — initialize
        cp850:   [0x1B, 0x74, 0x02],    // ESC t 2 — select PC850 (Latin-1 + Spanish)
        left:    [0x1B, 0x61, 0x00],
        center:  [0x1B, 0x61, 0x01],
        right:   [0x1B, 0x61, 0x02],
        boldOn:  [0x1B, 0x45, 0x01],
        boldOff: [0x1B, 0x45, 0x00],
        dblHOn:  [0x1D, 0x21, 0x01],    // double height
        dblHOff: [0x1D, 0x21, 0x00],
        lf:      [0x0A],
        cut:     [0x1D, 0x56, 0x41, 0x03] // GS V A 3 — partial cut + feed
    },

    _feed: function (n) { return [0x1B, 0x64, n || 1]; },

    _rule: function (w, ch) {
        const c = (ch || '-').charCodeAt(0);
        return new Array(w || 48).fill(c).concat([0x0A]);
    },

    // Encode a JS string to PC850 byte array
    _enc: function (str) {
        if (!str) return [];
        // PC850 mapping for characters outside ASCII
        const map = {
            'á': 0xA0, 'í': 0xA1, 'ó': 0xA2, 'ú': 0xA3,
            'ñ': 0xA4, 'Ñ': 0xA5, '¿': 0xA8, '¡': 0xAD,
            'é': 0x82, 'â': 0x83, 'ä': 0x84, 'à': 0x85,
            'å': 0x86, 'ç': 0x87, 'ê': 0x88, 'ë': 0x89,
            'è': 0x8A, 'ï': 0x8B, 'î': 0x8C, 'ì': 0x8D,
            'Ä': 0x8E, 'Å': 0x8F, 'É': 0x90, 'ô': 0x93,
            'ö': 0x94, 'ò': 0x95, 'û': 0x96, 'ù': 0x97,
            'Ö': 0x99, 'Ü': 0x9A, 'ü': 0x81,
            'Á': 0xB5, 'Â': 0xB6, 'À': 0xB7,
            'ã': 0xC6, 'Ã': 0xC7, 'õ': 0xE4, 'Õ': 0xE5,
            '€': 0xD5, '£': 0x9C
        };
        const out = [];
        for (let i = 0; i < str.length; i++) {
            const c = str.charCodeAt(i);
            if (c < 0x80) { out.push(c); }
            else { out.push(map[str[i]] !== undefined ? map[str[i]] : 0x3F); }
        }
        return out;
    },

    _line: function (str) {
        return window.thermalPrint._enc(str).concat([0x0A]);
    },

    _padEnd: function (str, w) {
        str = String(str == null ? '' : str);
        if (str.length > w) str = str.substring(0, w - 1) + '.';
        return str + ' '.repeat(Math.max(0, w - str.length));
    },

    _padStart: function (str, w) {
        str = String(str == null ? '' : str);
        if (str.length > w) str = str.substring(0, w);
        return ' '.repeat(Math.max(0, w - str.length)) + str;
    },

    _cur: function (n) { return '$' + Number(n || 0).toFixed(2); },

    // Append arrays into dst
    _add: function (dst) {
        for (let i = 1; i < arguments.length; i++) {
            const src = arguments[i];
            for (let j = 0; j < src.length; j++) dst.push(src[j]);
        }
        return dst;
    },

    // ── Raster image → ESC/POS bytes (GS v 0) ────────────────────────────

    /**
     * Converts a base64 image (or data URL) to ESC/POS raster bytes.
     * targetWidth: desired width in dots (printer dots, e.g. 300 for ~37mm on 203dpi).
     * Returns a flat byte array ready to splice into the output buffer.
     */
    _imageToEscPos: function (base64Src, targetWidth) {
        return new Promise(function (resolve) {
            const img = new Image();
            img.onload = function () {
                const w = targetWidth;
                const h = Math.round(img.height * (w / img.width));

                const canvas = document.createElement('canvas');
                canvas.width  = w;
                canvas.height = h;
                const ctx = canvas.getContext('2d');
                ctx.fillStyle = 'white';
                ctx.fillRect(0, 0, w, h);
                ctx.drawImage(img, 0, 0, w, h);

                const pixels = ctx.getImageData(0, 0, w, h).data;
                const bytesPerRow = Math.ceil(w / 8);
                const raster = [];

                for (let row = 0; row < h; row++) {
                    for (let col = 0; col < bytesPerRow; col++) {
                        let byte = 0;
                        for (let bit = 0; bit < 8; bit++) {
                            const x = col * 8 + bit;
                            if (x < w) {
                                const idx = (row * w + x) * 4;
                                const gray = 0.299 * pixels[idx] + 0.587 * pixels[idx + 1] + 0.114 * pixels[idx + 2];
                                if (gray < 128) byte |= (0x80 >> bit);
                            }
                        }
                        raster.push(byte);
                    }
                }

                const xL = bytesPerRow & 0xFF;
                const xH = (bytesPerRow >> 8) & 0xFF;
                const yL = h & 0xFF;
                const yH = (h >> 8) & 0xFF;
                resolve([0x1D, 0x76, 0x30, 0x00, xL, xH, yL, yH].concat(raster));
            };
            img.onerror = function () { resolve([]); };
            img.src = base64Src.startsWith('data:') ? base64Src : 'data:image/png;base64,' + base64Src;
        });
    },

    // ── QR Code ESC/POS sequence ───────────────────────────────────────────

    _qr: function (text) {
        const data = window.thermalPrint._enc(text);
        const len = data.length + 3;
        const pL = len & 0xFF;
        const pH = (len >> 8) & 0xFF;
        return [
            0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00, // model 2
            0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x04,        // module size 4
            0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31,        // error level M
            0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30].concat(data).concat([
            0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30         // print
        ]);
    },

    // ── Receipt builders ───────────────────────────────────────────────────

    _buildSale: async function (data) {
        const cfg = data.config;
        const s = data.sale;
        const W = 48;
        const t = window.thermalPrint;
        const add = t._add;
        const b = t._b;
        const out = [];

        add(out, b.init, b.cp850);

        // Custom header
        if (cfg.customHeader) {
            add(out, b.center);
            add(out, t._line(cfg.customHeader));
        }

        // Company logo
        if (cfg.showCompanyLogo && cfg.companyLogoBase64) {
            const logoBytes = await t._imageToEscPos(cfg.companyLogoBase64, 300);
            if (logoBytes.length > 0) {
                add(out, b.center);
                add(out, logoBytes);
                add(out, b.lf);
            }
        }

        // Company name — bold + double height
        add(out, b.center, b.boldOn, b.dblHOn);
        add(out, t._line(cfg.companyName));
        add(out, b.dblHOff, b.boldOff);

        if (cfg.companyAddress) add(out, t._line(cfg.companyAddress));
        if (cfg.companyPhone)   add(out, t._line('Tel: ' + cfg.companyPhone));
        if (cfg.companyTaxId)   add(out, t._line('RFC: ' + cfg.companyTaxId));

        add(out, b.left, t._rule(W));

        add(out, t._line('Ticket: #' + s.id));
        add(out, t._line('Fecha:  ' + s.saleDate));
        if (s.customerName) add(out, t._line('Cliente: ' + s.customerName));

        add(out, t._rule(W));

        // Column headers
        add(out, t._line(
            t._padEnd('Producto', 24) +
            t._padStart('Cant', 5) +
            t._padStart('Precio', 9) +
            t._padStart('Total', 10)
        ));
        add(out, t._rule(W));

        // Items
        for (let i = 0; i < s.items.length; i++) {
            const item = s.items[i];
            add(out, t._line(
                t._padEnd(item.name, 24) +
                t._padStart(String(item.quantity), 5) +
                t._padStart(t._cur(item.unitPrice), 9) +
                t._padStart(t._cur(item.total), 10)
            ));
        }

        add(out, t._rule(W));

        // Totals
        const pad = 38;
        add(out, t._line(t._padEnd('Subtotal:', pad) + t._padStart(t._cur(s.subtotal), W - pad)));
        if (s.discountAmount > 0)
            add(out, t._line(t._padEnd('Descuento:', pad) + t._padStart('-' + t._cur(s.discountAmount), W - pad)));
        add(out, t._line(t._padEnd('IVA:', pad) + t._padStart(t._cur(s.taxAmount), W - pad)));
        if (s.roundingAmount && s.roundingAmount !== 0)
            add(out, t._line(t._padEnd('Redondeo:', pad) + t._padStart(t._cur(s.roundingAmount), W - pad)));

        add(out, t._rule(W, '='));
        add(out, b.boldOn, b.dblHOn);
        add(out, t._line(t._padEnd('TOTAL:', pad) + t._padStart(t._cur(s.total), W - pad)));
        add(out, b.dblHOff, b.boldOff);
        add(out, t._rule(W));

        // Payments
        for (let j = 0; j < s.payments.length; j++) {
            const p = s.payments[j];
            add(out, t._line(t._padEnd(p.name + ':', pad) + t._padStart(t._cur(p.amount), W - pad)));
        }

        // QR code
        if (cfg.showQrCode && s.qrContent) {
            add(out, b.lf, b.center);
            add(out, t._qr(s.qrContent));
            add(out, b.left);
        }

        // Footer
        if (cfg.customFooter) {
            add(out, b.lf, b.center);
            add(out, t._line(cfg.customFooter));
            add(out, b.left);
        }

        add(out, t._feed(3), b.cut);
        return out;
    },

    _buildWithdrawal: function (data) {
        const cfg = data.config;
        const w = data.withdrawal;
        const W = 48;
        const t = window.thermalPrint;
        const add = t._add;
        const b = t._b;
        const out = [];

        add(out, b.init, b.cp850);
        add(out, b.center, b.boldOn, b.dblHOn);
        add(out, t._line(cfg.companyName));
        add(out, b.dblHOff, b.boldOff);
        if (cfg.companyAddress) add(out, t._line(cfg.companyAddress));

        add(out, t._rule(W));
        add(out, b.boldOn);
        add(out, t._line('RETIRO DE CAJA'));
        add(out, b.boldOff);
        add(out, t._rule(W));
        add(out, b.left);

        if (w.withdrawalNumber) add(out, t._line('No. Retiro: ' + w.withdrawalNumber));
        add(out, t._line('Fecha:      ' + w.createdAt));
        if (w.locationName) add(out, t._line('Ubicacion:  ' + w.locationName));
        if (w.cashierName)  add(out, t._line('Cajero:     ' + w.cashierName));

        add(out, t._rule(W));
        add(out, b.boldOn, b.dblHOn);
        add(out, t._line('Monto: ' + t._cur(w.amount)));
        add(out, b.dblHOff, b.boldOff);
        if (w.reason) add(out, t._line('Motivo: ' + w.reason));

        add(out, t._rule(W), b.lf, b.center);
        add(out, t._line('Firma: ________________________'));
        add(out, t._feed(3), b.cut);
        return out;
    },

    _buildTest: function () {
        const W = 48;
        const t = window.thermalPrint;
        const add = t._add;
        const b = t._b;
        const out = [];

        add(out, b.init, b.cp850, b.center, b.boldOn, b.dblHOn);
        add(out, t._line('PRUEBA DE IMPRESION'));
        add(out, b.dblHOff, b.boldOff);
        add(out, t._rule(W));
        add(out, b.left);
        add(out, t._line('Web Serial API OK'));
        add(out, t._line('Epson TM-T20IV'));
        add(out, t._line('Caracteres: aeiou AEIOU'));
        add(out, t._line('Especiales: \xE1\xE9\xED\xF3\xFA \xF1\xD1 \xA8\xAD'));
        add(out, t._rule(W));
        add(out, t._feed(3), b.cut);
        return out;
    }
};
