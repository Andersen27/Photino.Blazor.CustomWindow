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
function getElementBounds(element) {
    if (!element)
        return null;
    var bounds = element.getBoundingClientRect();
    return [bounds.left, bounds.top, bounds.width, bounds.height, window.devicePixelRatio];
}