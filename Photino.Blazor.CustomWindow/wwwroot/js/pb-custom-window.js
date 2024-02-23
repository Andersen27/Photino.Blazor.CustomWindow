/* disable window zoom */
window.addEventListener('mousewheel', e => { if (e.ctrlKey) e.preventDefault(); }, { passive: false });

export function invokeElementMethod(element, methodName, ...args) {
    if (element)
        element[methodName](args);
}

export function getElementPropertyValue(element, propertyName) {
    return element ? element[propertyName] : null;
}

export function setElementProperty(element, propertyName, value) {
    if (element)
        element[propertyName] = value;
}

export function getElementBounds(element) {
    if (!element) return null;
    var bounds = element.getBoundingClientRect();
    return [bounds.left, bounds.top, bounds.width, bounds.height];
}