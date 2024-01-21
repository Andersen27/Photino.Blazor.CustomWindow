function invokeElementMethod(element, methodName, ...args) {
    element[methodName](args);
}
function getElementPropertyValue(element, propertyName) {
    return element[propertyName];
}
function setElementProperty(element, propertyName, value) {
    element[propertyName] = value;
}
function getElementBounds(element) {
    var bounds = element.getBoundingClientRect();
    return [bounds.left, bounds.top, bounds.width, bounds.height];
}