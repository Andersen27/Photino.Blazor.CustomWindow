/* disable page zoom */
window.addEventListener(
    'mousewheel',
    e => {
        if (e.ctrlKey)
            e.preventDefault();
    },
    { passive: false }
);
window.addEventListener(
    'keydown',
    e => {
        if (e.ctrlKey == true && (
            e.keyCode == '61' || e.keyCode == '107' ||
            e.keyCode == '173' || e.keyCode == '109' ||
            e.keyCode == '187' || e.keyCode == '189')) {
            e.preventDefault();
        }
    }
);
