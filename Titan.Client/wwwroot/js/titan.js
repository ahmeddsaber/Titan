// ═══════════════════════════════════════════════════════
//  TITAN JavaScript Helpers
// ═══════════════════════════════════════════════════════
window.TitanJS = {

    // ── Language / RTL ──────────────────────────────────
    setLanguage(lang) {
        document.documentElement.lang = lang;
        document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
        localStorage.setItem('titan_lang', lang);
    },
    getLanguage() {
        return localStorage.getItem('titan_lang') || 'en';
    },

    // ── Cart Drawer ──────────────────────────────────────
    openCart() {
        document.getElementById('titan-cart')?.classList.add('open');
        document.querySelector('.cart-overlay')?.classList.add('active');
    },
    closeCart() {
        document.getElementById('titan-cart')?.classList.remove('open');
        document.querySelector('.cart-overlay')?.classList.remove('active');
    },

    // ── Splash ───────────────────────────────────────────
    hideSplash() {
        const el = document.getElementById('titan-splash');
        if (!el) return;
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.6s ease';
        setTimeout(() => el.remove(), 600);
    },

    // ── Navbar Scroll ─────────────────────────────────────
    initNavbarScroll() {
        window.addEventListener('scroll', () => {
            const nav = document.querySelector('.navbar');
            if (nav) nav.classList.toggle('scrolled', window.scrollY > 50);
        }, { passive: true });
    },

    // ── Scroll ───────────────────────────────────────────
    scrollToTop() { window.scrollTo({ top: 0, behavior: 'smooth' }); },
    scrollTo(id) { document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' }); },

    // ── Clipboard ─────────────────────────────────────────
    async copyToClipboard(text) {
        try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
    },

    // ── Image Lightbox ────────────────────────────────────
    openLightbox(src) {
        const overlay = document.createElement('div');
        overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,.96);z-index:9999;display:flex;align-items:center;justify-content:center;cursor:zoom-out';
        const img = document.createElement('img');
        img.src = src;
        img.style.cssText = 'max-width:90vw;max-height:90vh;object-fit:contain;border-radius:8px';
        overlay.appendChild(img);
        overlay.onclick = () => overlay.remove();
        document.body.appendChild(overlay);
    },

    // ── Social Share ──────────────────────────────────────
    shareWhatsApp(name, price, url) {
        const text = `🔱 *TITAN*\n${name}\nPrice: ${price}\n${url}`;
        window.open(`https://api.whatsapp.com/send?text=${encodeURIComponent(text)}`, '_blank');
    },
    shareFacebook(url) {
        window.open(`https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`, '_blank', 'width=600,height=400');
    },
    shareTelegram(name, url) {
        window.open(`https://t.me/share/url?url=${encodeURIComponent(url)}&text=${encodeURIComponent('TITAN — ' + name)}`, '_blank');
    },
    shareGeneric(name, url) {
        if (navigator.share) navigator.share({ title: `TITAN — ${name}`, url });
        else this.copyToClipboard(url);
    },

    // ── Print ─────────────────────────────────────────────
    printElement(id) {
        const el = document.getElementById(id);
        if (!el) return;
        const w = window.open('', '_blank');
        w.document.write(`<!DOCTYPE html><html><head><title>TITAN Order</title>
      <style>body{font-family:sans-serif;padding:24px}img{max-width:60px}table{width:100%;border-collapse:collapse}td,th{padding:8px;border:1px solid #ddd}</style>
    </head><body>${el.innerHTML}</body></html>`);
        w.document.close();
        w.print();
    },

    // ── Countdown ─────────────────────────────────────────
    startCountdown(elementId, expiresAt, dotnetRef) {
        const update = () => {
            const diff = new Date(expiresAt) - Date.now();
            if (diff <= 0) { dotnetRef.invokeMethodAsync('OnExpired'); return; }
            const h = String(Math.floor(diff / 3600000)).padStart(2, '0');
            const m = String(Math.floor((diff % 3600000) / 60000)).padStart(2, '0');
            const s = String(Math.floor((diff % 60000) / 1000)).padStart(2, '0');
            const el = document.getElementById(elementId);
            if (el) el.textContent = `${h}:${m}:${s}`;
        };
        update();
        return setInterval(update, 1000);
    },
    clearTimer(id) { clearInterval(id); },
};

// ── Auto init ─────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    TitanJS.initNavbarScroll();
    const lang = TitanJS.getLanguage();
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
});