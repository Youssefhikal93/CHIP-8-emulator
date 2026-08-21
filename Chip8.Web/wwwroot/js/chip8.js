// Render loop, keyboard input and beeper for the Blazor CHIP-8 emulator.
window.chip8 = (() => {
    const W = 64, H = 32;

    // Same layout as the desktop version: CHIP-8 key index -> keyboard key
    const KEYMAP = {
        x: 0, 1: 1, 2: 2, 3: 3,
        q: 4, w: 5, e: 6, a: 7,
        s: 8, d: 9, z: 10, c: 11,
        4: 12, r: 13, f: 14, v: 15,
    };

    let dotnetRef = null;
    let keys = 0;
    let initialized = false;

    let canvas, ctx, off, offCtx, imageData;
    let audioCtx = null, gainNode = null;

    function initCanvas() {
        canvas = document.getElementById('chip8-screen');
        ctx = canvas.getContext('2d');
        ctx.imageSmoothingEnabled = false;

        off = document.createElement('canvas');
        off.width = W;
        off.height = H;
        offCtx = off.getContext('2d');
        imageData = offCtx.createImageData(W, H);
    }

    function initInput() {
        window.addEventListener('keydown', (e) => {
            const k = KEYMAP[e.key.toLowerCase()];
            if (k !== undefined) {
                keys |= (1 << k);
                e.preventDefault();
            }
            ensureAudio();
        });
        window.addEventListener('keyup', (e) => {
            const k = KEYMAP[e.key.toLowerCase()];
            if (k !== undefined) keys &= ~(1 << k);
        });
        window.addEventListener('pointerdown', ensureAudio);
    }

    // Browsers only allow audio after a user gesture.
    function ensureAudio() {
        if (audioCtx) return;
        audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        const osc = audioCtx.createOscillator();
        osc.type = 'square';
        osc.frequency.value = 440;
        gainNode = audioCtx.createGain();
        gainNode.gain.value = 0;
        osc.connect(gainNode).connect(audioCtx.destination);
        osc.start();
    }

    function setBeep(on) {
        if (gainNode) gainNode.gain.value = on ? 0.04 : 0;
    }

    function draw(display) {
        // Blazor sends byte[] as a Uint8Array; older serializers use base64.
        const bytes = display instanceof Uint8Array
            ? display
            : Uint8Array.from(atob(display), (c) => c.charCodeAt(0));
        const px = imageData.data;
        for (let i = 0; i < W * H; i++) {
            const on = bytes[i] !== 0;
            px[i * 4 + 0] = on ? 255 : 10;   // amber phosphor, like the GL shader
            px[i * 4 + 1] = on ? 230 : 10;
            px[i * 4 + 2] = 0;
            px[i * 4 + 3] = 255;
        }
        offCtx.putImageData(imageData, 0, 0);
        ctx.imageSmoothingEnabled = false;
        ctx.drawImage(off, 0, 0, canvas.width, canvas.height);
    }

    async function loop() {
        if (!dotnetRef) return;
        try {
            const frame = await dotnetRef.invokeMethodAsync('Tick', keys);
            draw(frame.display);
            setBeep(frame.beep);
        } catch (err) {
            // Component disposed (e.g. page teardown) — stop the loop.
            console.error('chip8 loop stopped:', err);
            return;
        }
        requestAnimationFrame(loop);
    }

    return {
        start(ref) {
            dotnetRef = ref;
            if (!initialized) {
                initCanvas();
                initInput();
                initialized = true;
            }
            requestAnimationFrame(loop);
        },
    };
})();
