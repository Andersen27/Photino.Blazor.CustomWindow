# ![ ](customwindow.png) Photino.Blazor.CustomWindow
![ ](preview.png)

## About
Customizable cross-platform window view and behaviour implementation for Photino.Blazor applications with Chromeless mode.\
The project provides Blazor component named `CustomWindow` that takes up the entire space of the cromeless window and renders custom window header and borders.
It includes the possibility of customizing the window header up to the icon, title and control buttons, moving the window on header dragging, window resizing on borders dragging, and some other features such as stretching (expanding) the window by half the screen when moving it to the boundaries of the monitor's work area.
Default control buttons allow to minimize, maximize and close the window. At the same time, the component also provides appropriate methods.

## How to use
1. Include **Photino.Blazor.CustomWindow** as PackageReference to your project.
2. Copy files from [Photino.Blazor.CustomWindow/wwwroot](Photino.Blazor.CustomWindow/wwwroot) folder to your wwwroot.
3. Use `CustomWindow` component as root in your markup and place your content to its `WindowContent` RenderFragment (see [example](Photino.Blazor.CustomWindow.Sample/Shared/MainLayout.razor)).
4. Don't forget to set the `PhotinoWindow.Chromeless` property to true.

## Next steps
- At the moment the project works correctly only for screens with a 100% zoom factor. I'm waiting for the opportunity to track the monitor scale factor in Photino ([Issue](https://github.com/tryphotino/photino.Blazor/issues/105)). It’s the main reason why the project is in alpha version.
