/* Modifica senza merge dal progetto 'Photino.Blazor.CustomWindow (net7.0)'
Prima:
using System;
Dopo:
using Microsoft.AspNetCore.Components;
using System;
*/

using Microsoft.AspNetCore.Components;

/* Modifica senza merge dal progetto 'Photino.Blazor.CustomWindow (net7.0)'
Prima:
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
Dopo:
using System.Threading.Tasks;
*/

using System.Drawing;

namespace Microsoft.JSInterop;

// Global JavaScript functions
/*
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
    return [rect.left, rect.top, rect.width, rect.height];
}
*/

/// <summary>
/// Extensions for <see cref="IJSObjectReference"/>.
/// </summary>
public static class JSRuntimeExtensions
{
    /// <summary>
    /// Invokes the specified JavaScript method of given <see cref="ElementReference" />.<br/>
    /// Global JavaScript function <c>invokeElementMethod(element, methodName, args)</c> must declared.
    /// </summary>
    /// <param name="module">The <see cref="IJSObjectReference"/>.</param>
    /// <param name="methodName">An identifier for the method to invoke.</param>
    /// <param name="args">Arguments passed to method.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous invocation operation.</returns>
    public static async ValueTask InvokeElementMethodAsync(this IJSObjectReference module, ElementReference element, string methodName, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(module);

        await module.InvokeVoidAsync("invokeElementMethod", element, methodName, args);
    }

    /// <summary>
    /// Gets the specified JavaScript property of given <see cref="ElementReference" />.<br/>
    /// Global JavaScript function <c>getElementPropertyValue(element, propertyName)</c> must declared.
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="module">The <see cref="IJSObjectReference"/>.</param>
    /// <param name="propertyName">An identifier for the property to get.</param>
    /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
    public static async ValueTask<TValue> GetElementPropertyValueAsync<TValue>(this IJSObjectReference module, ElementReference element, string propertyName)
    {
        return await module.InvokeAsync<TValue>("getElementPropertyValue", element, propertyName);
    }

    /// <summary>
    /// Sets the specified JavaScript property of given <see cref="ElementReference" />.<br/>
    /// Global JavaScript function <c>setElementProperty(element, propertyName, value)</c> must declared.
    /// </summary>
    /// <param name="module">The <see cref="IJSObjectReference"/>.</param>
    /// <param name="propertyName">An identifier for the property to set value.</param>
    /// <param name="value">A value for the property to set.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous invocation operation.</returns>
    public static async ValueTask SetElementPropertyAsync(this IJSObjectReference module, ElementReference element, string propertyName, object value)
    {
        await module.InvokeVoidAsync("setElementProperty", element, propertyName, value);
    }

    /// <summary>
    /// Gets given <see cref="ElementReference" /> bounds rectangle, received by getBoundingClientRect() JavaScript method.<br/>
    /// Global JavaScript function <c>getElementBounds(element)</c> must declared.
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="module">The <see cref="IJSObjectReference"/>.</param>
    /// <returns>Given <see cref="ElementReference" /> bounds rectangle, converted to <see cref="Rectangle"/> type</returns>
    public static async ValueTask<Rectangle> GetElementBounds(this IJSObjectReference module, ElementReference element, bool returnScaled = false)
    {
        var bounds = await module.InvokeAsync<double[]>("getElementBounds", element, returnScaled);
        return bounds is null ? Rectangle.Empty : new Rectangle((int)bounds[0], (int)bounds[1], (int)bounds[2], (int)bounds[3]);
    }
}