function invokeElementMethod(element, methodName, ...args) {
    if (element)
        element[methodName](args);
}
function getElementPropertyValue(element, propertyName) {
    return element ? element[propertyName] : null;
}
function setElementProperty(element, propertyName, value) {
    if (element)
        element[propertyName] = value;
}
function getElementBounds(element, returnScaled) {
    if (!element) return null;
    var scale = returnScaled ? window.devicePixelRatio : 1;
    var bounds = element.getBoundingClientRect();
    return [bounds.left * scale, bounds.top * scale,
    bounds.width * scale, bounds.height * scale];
}