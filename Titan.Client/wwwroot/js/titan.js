// ─── TITAN JavaScript Helpers ─────────────────────────────────────────────────

window.TitanJS = {

    // ─── Local Storage ────────────────────────────────────────────────────────
    setItem: (key, value) => localStorage.setItem(key, value),
    getItem: (key) => localStorage.getItem(key),
    removeItem: (key) => localStorage.removeItem(key),

    // ─── Session Storage ──────────────────────────────────────────────────────
    setSession: (key, value) => sessionStorage.setItem(key, value),
    getSession: (key) => sessionStorage.getItem(key),

    // ─── Scroll ───────────────────────────────────────────────────────────────
    scrollToTop: () => window.scrollTo({ top: 0, behavior: 'smooth' }),
    scrollTo: (id) => {
        const el = document.getElementById(id);
        if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    },
    getScrollY: () => window.scrollY,

    // ─── Clipboard ────────────────────────────────────────────────────────────
    copyToClipboard: async (text) => {
        try { await navigator.clipboard.writeText(text); return true; }
        catch { return false; }
    },

    // ─── DOM ──────────────────────────────────────────────────────────────────
    focusElement: (id) => { const el = document.getElementById(id); if (el) el.focus(); },
    addClass: (id, cls) => document.getElementById(id)?.classList.add(cls),
    removeClass: (id, cls) => document.getElementById(id)?.classList.remove(cls),
    toggleClass: (id, cls) => document.getElementById(id)?.classList.toggle(cls),

    // ─── Language / RTL ───────────────────────────────────────────────────────
    setLanguage: (lang) => {
        document.documentElement.lang = lang;
        document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
        localStorage.setItem('titan_lang', lang);
    },
    getLanguage: () => localStorage.getItem('titan_lang') || 'en',

    // ─── Theme ────────────────────────────────────────────────────────────────
    initNavbarScroll: () => {
        window.addEventListener('scroll', () => {
            const nav = document.querySelector('.navbar');
            if (nav) nav.classList.toggle('scrolled', window.scrollY > 50);
        });
    },

    // ─── Splash ───────────────────────────────────────────────────────────────
    hideSplash: () => {
        const splash = document.getElementById('splash');
        if (splash) {
            splash.style.opacity = '0';
            splash.style.transition = 'opacity 0.5s ease';
            setTimeout(() => splash.remove(), 500);
        }
    },

    // ─── Share ────────────────────────────────────────────────────────────────
    shareProduct: (name, url) => {
        if (navigator.share) {
            navigator.share({ title: `TITAN — ${name}`, url });
        } else {
            window.open(`https://api.whatsapp.com/send?text=${encodeURIComponent(`Check out ${name} on TITAN: ${url}`)}`, '_blank');
        }
    },
    shareWhatsApp: (name, price, url) =>
        window.open(`https://api.whatsapp.com/send?text=${encodeURIComponent(`🔱 *TITAN*\n${name}\nPrice: ${price}\n${url}`)}`, '_blank'),
    shareFacebook: (url) =>
        window.open(`https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`, '_blank', 'width=600,height=400'),
    shareInstagram: () => window.open('https://instagram.com/titan_brand', '_blank'),
    shareTelegram: (name, url) =>
        window.open(`https://t.me/share/url?url=${encodeURIComponent(url)}&text=${encodeURIComponent(`TITAN — ${name}`)}`, '_blank'),

    // ─── Image Preview ────────────────────────────────────────────────────────
    openLightbox: (src) => {
        const overlay = document.createElement('div');
        overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.95);z-index:9999;display:flex;align-items:center;justify-content:center;cursor:zoom-out;';
        const img = document.createElement('img');
        img.src = src;
        img.style.cssText = 'max-width:90vw;max-height:90vh;object-fit:contain;border:1px solid #222;';
        overlay.appendChild(img);
        overlay.onclick = () => overlay.remove();
        document.body.appendChild(overlay);
    },

    // ─── Countdown Timer ──────────────────────────────────────────────────────
    startCountdown: (elementId, expiresAt, dotnetRef) => {
        const update = () => {
            const diff = new Date(expiresAt) - Date.now();
            if (diff <= 0) { dotnetRef.invokeMethodAsync('OnCountdownExpired'); return; }
            const h = Math.floor(diff / 3600000).toString().padStart(2, '0');
            const m = Math.floor((diff % 3600000) / 60000).toString().padStart(2, '0');
            const s = Math.floor((diff % 60000) / 1000).toString().padStart(2, '0');
            const el = document.getElementById(elementId);
            if (el) el.textContent = `${h}:${m}:${s}`;
        };
        update();
        return setInterval(update, 1000);
    },
    clearTimer: (id) => clearInterval(id),

    // ─── Print ────────────────────────────────────────────────────────────────
    printElement: (id) => {
        const el = document.getElementById(id);
        if (!el) return;
        const w = window.open('', '_blank');
        w.document.write(`<html><head><title>TITAN Order</title></head><body>${el.innerHTML}</body></html>`);
        w.document.close();
        w.print();
    },

    // ─── Chart Helpers ────────────────────────────────────────────────────────
    drawSparkline: (canvasId, data, color = '#D4AF37') => {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        const w = canvas.width, h = canvas.height;
        const max = Math.max(...data);
        const min = Math.min(...data);
        const range = max - min || 1;
        ctx.clearRect(0, 0, w, h);
        ctx.beginPath();
        ctx.strokeStyle = color;
        ctx.lineWidth = 2;
        data.forEach((v, i) => {
            const x = (i / (data.length - 1)) * w;
            const y = h - ((v - min) / range) * h;
            i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
        });
        ctx.stroke();
        // Fill
        ctx.lineTo(w, h); ctx.lineTo(0, h);
        ctx.fillStyle = color + '22';
        ctx.fill();
    }
};

// ─── Auto-init ────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    TitanJS.initNavbarScroll();
    const savedLang = TitanJS.getLanguage();
    TitanJS.setLanguage(savedLang);
});