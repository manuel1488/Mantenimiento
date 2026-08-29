window.signaturePad = (function () {
    const _drawn = {};

    function init(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        // The canvas has no width/height attributes — sizing it here from its rendered CSS box
        // (instead of a fixed HTML attribute like width="600") avoids the dialog widening past the
        // viewport on narrow phones, since a fixed intrinsic canvas size fights the CSS width:100%.
        const rect = canvas.getBoundingClientRect();
        canvas.width = Math.round(rect.width);
        canvas.height = Math.round(rect.height);

        const ctx = canvas.getContext('2d');
        let drawing = false;
        _drawn[canvasId] = false;

        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        canvas.style.touchAction = 'none';
        ctx.strokeStyle = '#212121';
        ctx.lineWidth = 2.5;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';

        function getPos(e) {
            const rect = canvas.getBoundingClientRect();
            const scaleX = canvas.width / rect.width;
            const scaleY = canvas.height / rect.height;
            const src = e.touches ? e.touches[0] : e;
            return {
                x: (src.clientX - rect.left) * scaleX,
                y: (src.clientY - rect.top) * scaleY
            };
        }

        function onStart(e) {
            drawing = true;
            _drawn[canvasId] = true;
            const pos = getPos(e);
            ctx.beginPath();
            ctx.moveTo(pos.x, pos.y);
            e.preventDefault();
        }

        function onMove(e) {
            if (!drawing) return;
            const pos = getPos(e);
            ctx.lineTo(pos.x, pos.y);
            ctx.stroke();
            ctx.beginPath();
            ctx.moveTo(pos.x, pos.y);
            e.preventDefault();
        }

        function onEnd() { drawing = false; }

        canvas.addEventListener('mousedown', onStart);
        canvas.addEventListener('mousemove', onMove);
        canvas.addEventListener('mouseup', onEnd);
        canvas.addEventListener('mouseleave', onEnd);
        canvas.addEventListener('touchstart', onStart, { passive: false });
        canvas.addEventListener('touchmove', onMove, { passive: false });
        canvas.addEventListener('touchend', onEnd);
    }

    function clear(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        _drawn[canvasId] = false;
    }

    function getData(canvasId) {
        const canvas = document.getElementById(canvasId);
        return canvas ? canvas.toDataURL('image/png') : null;
    }

    function hasSignature(canvasId) {
        return !!_drawn[canvasId];
    }

    return { init, clear, getData, hasSignature };
})();
