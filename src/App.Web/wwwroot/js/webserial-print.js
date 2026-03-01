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
 */
window.thermalPrint = {

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

    printSale: async function (data) {
        return window.thermalPrint._send(window.thermalPrint._buildSale(data));
    },

    printWithdrawal: async function (data) {
        return window.thermalPrint._send(window.thermalPrint._buildWithdrawal(data));
    },

    printTest: async function () {
        return window.thermalPrint._send(window.thermalPrint._buildTest());
    },

    // ── Serial port send ───────────────────────────────────────────────────

    _send: async function (bytes) {
        try {
            const ports = await navigator.serial.getPorts();
            if (ports.length === 0) return false;

            const port = ports[0];
            await port.open({ baudRate: 9600 });

            const writer = port.writable.getWriter();
            await writer.write(new Uint8Array(bytes));
            writer.releaseLock();

            // Allow buffer to flush before closing
            await new Promise(r => setTimeout(r, 300));
            await port.close();
            return true;
        } catch (e) {
            console.error('[thermalPrint] send error:', e);
            return false;
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

    _buildSale: function (data) {
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
