using Microsoft.AspNetCore.Components;
using System.Drawing;

namespace Microsoft.JSInterop;

// Global JavaScript functions
/*
function invokeElementMethod(element, methodName, args) {
    if (element)
        element[methodName](...args);
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
*/

/// <summary>
/// Extensions for <see cref="IJSRuntime"/>.
/// </summary>
public static class JSRuntimeExtensions
{
    /// <summary>
    /// Invokes the specified JavaScript method of given <see cref="ElementReference" />.<br/>
    /// Global JavaScript function <c>invokeElementMethod(element, methodName, args)</c> must declared.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    /// <param name="methodName">An identifier for the method to invoke.</param>
    /// <param name="args">Arguments passed to method.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous invocation operation.</returns>
    public static async ValueTask InvokeElementMethodAsync(this IJSRuntime jsRuntime, ElementReference element, string methodName, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        await jsRuntime.InvokeVoidAsync("invokeElementMethod", element, methodName, args);
    }

    /// <summary>
    /// Gets the specified JavaScript property of given <see cref="ElementReference" />.<br/>
    /// Global JavaScript function <c>getElementPropertyValue(element, propertyName)</c> must declared.
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    /// <param name="propertyName">An identifier for the property to get.</param>
    /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
    public static async ValueTask<TValue> GetElementPropertyValueAsync<TValue>(this IJSRuntime jsRuntime, ElementReference element, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        return await jsRuntime.InvokeAsync<TValue>("getElementPropertyValue", element, propertyName);
    }

    /// <summary>
    /// Sets the specified JavaScript property of given <see cref="ElementReference" />.<br/>
    /// Global JavaScript function <c>setElementProperty(element, propertyName, value)</c> must declared.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    /// <param name="propertyName">An identifier for the property to set value.</param>
    /// <param name="value">A value for the property to set.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous invocation operation.</returns>
    public static async ValueTask SetElementPropertyAsync(this IJSRuntime jsRuntime, ElementReference element, string propertyName, object value)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        await jsRuntime.InvokeVoidAsync("setElementProperty", element, propertyName, value);
    }

    /// <summary>
    /// Gets given <see cref="ElementReference" /> bounds rectangle, received by getBoundingClientRect() JavaScript method.<br/>
    /// Global JavaScript function <c>getElementBounds(element)</c> must declared.
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    /// <returns>Given <see cref="ElementReference" /> bounds rectangle, converted to <see cref="Rectangle"/> type</returns>
    public static async ValueTask<(Rectangle, double)> GetElementBounds(this IJSRuntime jsRuntime, ElementReference element, bool returnScaled = false)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        
        var bounds = await jsRuntime.InvokeAsync<double[]>("getElementBounds", element);
        if (bounds is null)
            return (Rectangle.Empty, -1);

        var scaleFactor = bounds[4];
        var boundsScale = returnScaled ? scaleFactor : 1.0;
        return (new Rectangle((int)(bounds[0] * boundsScale), (int)(bounds[1] * boundsScale),
                              (int)(bounds[2] * boundsScale), (int)(bounds[3] * boundsScale)), scaleFactor);
    }
}
