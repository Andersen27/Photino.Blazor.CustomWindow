/* disable window zoom */
window.addEventListener('mousewheel', e => { if (e.ctrlKey) e.preventDefault(); }, { passive: false });