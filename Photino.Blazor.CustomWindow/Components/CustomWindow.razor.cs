using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PhotinoNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Photino.Blazor.CustomWindow.Components;

/// <summary>
/// A component for displaying a customizable window header instead of the default from OS.
/// Implements standard OS window behavior: dragging, resizing, standard control buttons and more.
/// Should be used as the root component. Requires setting <see cref="PhotinoWindow.Chromeless"/> = <c>true</c>.
/// </summary>
public sealed partial class CustomWindow
{
    #region Internal types
    private enum ResizeThumb
    {
        Top,
        Bottom,
        Left,
        Right,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    private class ScreenInfo
    {
        public Rectangle OriginalArea { get; set; }
        public double ScaleFactor { get; set; }
        public int ActualLeft { get; set; }
        public int ActualTop { get; set; }
        public bool PositionActualized { get; set; } = false;
    }
    #endregion

    private static HashSet<PhotinoWindow> _allInitedWindows = new();
    private static List<ScreenInfo> _screensInfo;

    private ElementReference headerDragArea;
    private ElementReference resizeThumbLeft, resizeThumbRight,
                             resizeThumbTop, resizeThumbBottom,
                             resizeThumbTopLeft, resizeThumbTopRight,
                             resizeThumbBottomLeft, resizeThumbBottomRight;

    private ElementReference _activeResizeThumbArea;
    private ResizeThumb _activeResizeThumb;
    private bool _movingProcess;
    private bool _maximized;
    private bool _expanded;
    private bool _focused;
    private Point _headerPointerOffset;
    private Point _restoreLocation;
    private Size _restoreSize;

    private PhotinoWindow Window => PhotinoBlazorApp.MainWindow;
    private string IconSource => Icon ?? Path.GetFileName(Window.IconFile);
    private bool ResizeAvailable { get; set; } = true;

    [Inject]
    private PhotinoBlazorApp PhotinoBlazorApp { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    #region Parameters
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; }

    /// <summary>
    /// Inline CSS-style for root element.
    /// </summary>
    [Parameter]
    public string Style { get; set; }

    /// <summary>
    /// Razor layout inside window.
    /// </summary>
    [Parameter]
    public RenderFragment WindowContent { get; set; }

    /// <summary>
    /// Razor layout for left side of window header. By default - window icon and title.
    /// </summary>
    [Parameter]
    public RenderFragment HeaderMainLayout { get; set; } = null;

    /// <summary>
    /// Razor layout for central part of window header. By default - empty.
    /// This is the main window drag area, so it's recommended not to overlay it
    /// with elements that have pointer events propagation.
    /// </summary>
    [Parameter]
    public RenderFragment HeaderCentralLayout { get; set; } = null;

    /// <summary>
    /// Razor layout for right side of window header. By default - three standard control buttons:
    /// minimize, maximize/restore and close.
    /// </summary>
    [Parameter]
    public RenderFragment HeaderControlsLayout { get; set; } = null;

    /// <summary>
    /// Razor layout for displaying to the left of the <see cref="HeaderControlsLayout" />.
    /// A convenient way to add buttons with additional functionality, such as profile management.
    /// </summary>
    [Parameter]
    public RenderFragment HeaderExtraControlsLayout { get; set; } = null;

    /// <summary>
    /// Razor layout for default minimize button instead of default svg
    /// </summary>
    [Parameter]
    public RenderFragment MinimizeButtonContent { get; set; } = null;

    /// <summary>
    /// Razor layout for default maximize button instead of default svg
    /// </summary>
    [Parameter]
    public RenderFragment MaximizeButtonContent { get; set; } = null;

    /// <summary>
    /// Razor layout for default restore button instead of default svg
    /// </summary>
    [Parameter]
    public RenderFragment RestoreButtonContent { get; set; } = null;

    /// <summary>
    /// Razor layout for default close button instead of default svg
    /// </summary>
    [Parameter]
    public RenderFragment CloseButtonContent { get; set; } = null;

    /// <summary>
    /// Path for icon image source relative to wwwroot folder.
    /// By default - name of file from <see cref="PhotinoWindow.IconFile"/>.
    /// </summary>
    [Parameter]
    public string Icon { get; set; } = null;

    /// <summary>
    /// CSS-based color of custom window. This includes header background and borders color.
    /// </summary>
    [Parameter]
    public string WindowColor { get; set; } = "white";

    /// <summary>
    /// CSS-based color of header title text.
    /// </summary>
    [Parameter]
    public string TitleColor { get; set; } = "black";

    /// <summary>
    /// Minimum allowable window size.
    /// </summary>
    [Parameter]
    public Size MinSize { get; set; } = new Size(150, 150);

    /// <summary>
    /// Maximum allowable window size.
    /// </summary>
    [Parameter]
    public Size MaxSize { get; set; } = new Size(int.MaxValue, int.MaxValue);

    /// <summary>
    /// Is window resizable by borders or not.
    /// </summary>
    [Parameter]
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Window title. By default - <see cref="PhotinoWindow.Title"/>.
    /// </summary>
    [Parameter]
    public string Title
    {
        get => Window.Title;
        set => Window.Title = value;
    }

    /// <summary>
    /// Window location relative to entire screen top left point.
    /// </summary>
    [Parameter]
    public Point Location
    {
        get => Window.Location;
        set
        {
            var location = Window.Location;
            if (!location.Equals(value))
            {
                value = new Point(value.X, Math.Max(0, value.Y));
                Window.Location = value;
                LocationChanged.InvokeAsync(value);
            }
        }
    }

    /// <summary>
    /// Window location changed event callback. Passes the new location value.
    /// </summary>
    [Parameter]
    public EventCallback<Point> LocationChanged { get; set; }

    /// <summary>
    /// Window size.
    /// </summary>
    [Parameter]
    public Size Size
    {
        get => Window.Size;
        set
        {
            var size = Window.Size;
            value = new Size(Math.Min(MaxSize.Width, Math.Max(MinSize.Width, value.Width)),
                             Math.Min(MaxSize.Height, Math.Max(MinSize.Height, value.Height)));
            if (!size.Equals(value))
            {
                Window.Size = value;
                SizeChanged.InvokeAsync(value);
            }
        }
    }

    /// <summary>
    /// Window size changed event callback. Passes the new size value.
    /// </summary>
    [Parameter]
    public EventCallback<Size> SizeChanged { get; set; }

    /// <summary>
    /// Is window maximized or not. Receives the value of <see cref="PhotinoWindow.Maximized"/> on component initialize.
    /// </summary>
    [Parameter]
    public bool Maximized
    {
        get => _maximized;
        set
        {
            if (!_maximized.Equals(value))
            {
                if (value)
                {
                    _restoreLocation = Location;
                    _restoreSize = Size;
                    var screenArea = PhotinoBlazorApp.MainWindow.MainMonitor.WorkArea;
                    Size = screenArea.Size;
                    Location = screenArea.Location;
                }
                else
                {
                    Size = _restoreSize;
                    Location = _restoreLocation;
                }

                ResizeAvailable = !value;
                _maximized = value;
                _expanded = value;
                MaximizedChanged.InvokeAsync(value);
            }
        }
    }

    /// <summary>
    /// Window maximized changed event. Passes the new maximized value.
    /// </summary>
    [Parameter]
    public EventCallback<bool> MaximizedChanged { get; set; }

    /// <summary>
    /// Is window minimized or not.
    /// </summary>
    [Parameter]
    public bool Minimized
    {
        get => Window.Minimized;
        set => Window.Minimized = value;
    }

    /// <summary>
    /// Is window topmost or not.
    /// </summary>
    [Parameter]
    public bool Topmost
    {
        get => Window.Topmost;
        set => Window.Topmost = value;
    }

    /// <summary>
    /// Custom header height in screen pixels.
    /// </summary>
    [Parameter]
    public uint HeaderHeight { get; set; } = 28;

    /// <summary>
    /// Invisible resize area width along window borders in screen pixels.
    /// </summary>
    [Parameter]
    public uint ResizeAreaWidth { get; set; } = 6;

    /// <summary>
    /// Threshold for expand window when moving it towards the bounds of the monitor work area.
    /// </summary>
    [Parameter]
    public uint BordersExpandThreshold { get; set; } = 8;

    /// <summary>
    /// Is icon from default <see cref="HeaderMainLayout"/> should be displayed or not.
    /// </summary>
    [Parameter]
    public bool ShowIcon { get; set; } = true;

    /// <summary>
    /// Is window expand when moving it towards the bounds of the monitor work area enabled or not.
    /// </summary>
    [Parameter]
    public bool EnableExpand { get; set; } = true;

    /// <summary>
    /// Maximize/restore window on header double click or not.
    /// </summary>
    [Parameter]
    public bool MaximizeOnHeaderDoubleClick { get; set; } = true;

    /// <summary>
    /// Window closing event callback. Should return <c>bool</c> value, that means does window closing canceled or not.
    /// </summary>
    [Parameter]
    public WindowClosingHandler WindowClosingCallback { get; set; }
    #endregion

    #region Public members
    /// <summary>
    /// Is window expanded or not.
    /// </summary>
    public bool Expanded => _expanded;

    /// <summary>
    /// Is window focused or not.
    /// </summary>
    public bool Focused => _focused;

    /// <summary>
    /// Window location changed event. Passes the new location value.
    /// </summary>
    public event WindowLocationChangedHandler WindowLocationChanged;

    /// <summary>
    /// Window size changed event. Passes the new size value.
    /// </summary>
    public event WindowSizeChangedHandler WindowSizeChanged;

    /// <summary>
    /// Window maximized changed event. Passes the new maximized value.
    /// </summary>
    public event WindowMaximizedHandler WindowMaximized;

    /// <summary>
    /// Window minimized event.
    /// </summary>
    public event WindowMinimizedHandler WindowMinimized;

    /// <summary>
    /// Window closing event. Should return <c>bool</c> value, that means does window closing canceled or not.
    /// </summary>
    public event WindowClosingHandler WindowClosing;

    /// <summary>
    /// Window moving event. Passes <see cref="PointerEventArgs"/> of header moving process.
    /// </summary>
    public event WindowMovingHandler WindowMoving;

    /// <summary>
    /// Window move begin event.
    /// </summary>
    public event Action WindowMoveBegin;

    /// <summary>
    /// Window move end event.
    /// </summary>
    public event Action WindowMoveEnd;

    /// <summary>
    /// Window focus receive event.
    /// </summary>
    public event Action WindowFocusIn;

    /// <summary>
    /// Window focus lost event.
    /// </summary>
    public event Action WindowFocusOut;

    public delegate void WindowLocationChangedHandler(Point location);
    public delegate void WindowSizeChangedHandler(Size size);
    public delegate void WindowMaximizedHandler(bool maximized);
    public delegate void WindowMinimizedHandler();
    public delegate bool WindowClosingHandler();
    public delegate void WindowMovingHandler(PointerEventArgs e);
    #endregion

    protected override async Task OnInitializedAsync()
    {
        if (_screensInfo is null)
            await UpdateScreensInfo();

        if (_allInitedWindows.Contains(Window))
            return;

        if (!Window.Chromeless)
            throw new ApplicationException("PhotinoWindow.Chromeless property should be set to true before the native window is instantiated.");

        if (Window.Maximized)
            Maximized = true;
        // can't init Resizable because is's always false in chromeless mode
        //Resizable = Window.Resizable;
        // can't init MinSize because PhotinoWindow don't store it's value
        //MinSize = Window.MinSize;
        // can't init MaxSize because PhotinoWindow don't store it's value
        //MaxSize = Window.MaxSize;

        Window.WindowLocationChanged += (_, location) => WindowLocationChanged?.Invoke(location);
        Window.WindowSizeChanged += (_, size) => WindowSizeChanged?.Invoke(size);
        Window.WindowMaximized += (_, _) => WindowMaximized?.Invoke(Window.Maximized);
        Window.WindowMinimized += (_, _) => WindowMinimized?.Invoke();
        Window.WindowClosing += (_, _) => OnWindowClosing();
        Window.WindowFocusIn += (_, _) => { _focused = true; WindowFocusIn?.Invoke(); };
        Window.WindowFocusOut += (_, _) => { _focused = false; WindowFocusOut?.Invoke(); };
        _allInitedWindows.Add(Window);
    }

    private bool OnWindowClosing()
    {
        var cancelByCallback = WindowClosingCallback?.Invoke() ?? false;
        var cancelByEvent = WindowClosing?.GetInvocationList().Cast<WindowClosingHandler>()
            .Select(d => d.Invoke()).ToArray().Any(c => c) ?? false;

        var cancelClosing = cancelByCallback || cancelByEvent;
        if (!cancelClosing)
            _allInitedWindows.Remove(Window);
        return cancelClosing;
    }

    private Point GetActualPointerPosition(PointerEventArgs e)
    {
        var pointerScreenPos = new Point((int)e.ScreenX, (int)e.ScreenY);
        var screen = _screensInfo.First(s => s.OriginalArea.Contains(pointerScreenPos));
        pointerScreenPos.Offset(-screen.OriginalArea.Left, -screen.OriginalArea.Top);
        return new Point(screen.ActualLeft + (int)(pointerScreenPos.X * screen.ScaleFactor),
                         screen.ActualTop + (int)(pointerScreenPos.Y * screen.ScaleFactor));
    }

    private Rectangle GetPointerGlobalWorkArea(Point pointerScreenPos)
    {
        var monitor = Window.Monitors.Single(m => m.MonitorArea.Contains(pointerScreenPos));
        var workArea = monitor.WorkArea;
        workArea.Offset(monitor.MonitorArea.Location);
        return workArea;
    }

    private void UpdateMoveExpand(PointerEventArgs e)
    {
        var pointerScreenPos = GetActualPointerPosition(e);
        var workArea = GetPointerGlobalWorkArea(pointerScreenPos);

        if (pointerScreenPos.X < workArea.X + BordersExpandThreshold &&
            pointerScreenPos.Y < workArea.Y + BordersExpandThreshold)
        {
            _expanded = true;
            _restoreSize = Size;
            Size = new Size(workArea.Size.Width / 2, workArea.Size.Height / 2);
            Location = workArea.Location;
        }
        else if (pointerScreenPos.X > workArea.Location.X + workArea.Width - 1 - BordersExpandThreshold  &&
                 pointerScreenPos.Y < workArea.Y + BordersExpandThreshold)
        {
            _expanded = true;
            _restoreSize = Size;
            Size = new Size(workArea.Size.Width / 2, workArea.Size.Height / 2);
            Location = new Point(workArea.Location.X + workArea.Size.Width / 2, workArea.Location.Y);
        }
        else if (pointerScreenPos.X < workArea.X + BordersExpandThreshold &&
                 pointerScreenPos.Y > workArea.Location.Y + workArea.Height - 1 - BordersExpandThreshold)
        {
            _expanded = true;
            _restoreSize = Size;
            Size = new Size(workArea.Size.Width / 2, workArea.Size.Height / 2);
            Location = new Point(workArea.Location.X, workArea.Location.Y + workArea.Size.Height / 2);
        }
        else if (pointerScreenPos.X > workArea.Location.X + workArea.Width - 1 - BordersExpandThreshold &&
                 pointerScreenPos.Y > workArea.Location.Y + workArea.Height - 1 - BordersExpandThreshold)
        {
            _expanded = true;
            _restoreSize = Size;
            Size = new Size(workArea.Size.Width / 2, workArea.Size.Height / 2);
            Location = new Point(workArea.Location.X + workArea.Size.Width / 2, workArea.Location.Y + workArea.Size.Height / 2);
        }
        else if (pointerScreenPos.X < workArea.X + BordersExpandThreshold)
        {
            _expanded = true;
            _restoreSize = Size;
            Size = new Size(workArea.Size.Width / 2, workArea.Size.Height);
            Location = workArea.Location;
        }
        else if (pointerScreenPos.X > workArea.Location.X + workArea.Width - 1 - BordersExpandThreshold)
        {
            _expanded = true;
            _restoreSize = Size;
            Size = new Size(workArea.Size.Width / 2, workArea.Size.Height);
            Location = new Point(workArea.Location.X + workArea.Size.Width / 2, workArea.Location.Y);
        }
        else if (pointerScreenPos.Y < workArea.Y + BordersExpandThreshold)
        {
            Maximized = true;
        }
        else if (pointerScreenPos.Y > workArea.Location.Y + workArea.Height - 1 - BordersExpandThreshold)
        {
            _expanded = true;
            _restoreSize = Size;
            Size = new Size(workArea.Size.Width, workArea.Size.Height / 2);
            Location = new Point(workArea.Location.X, workArea.Location.Y + workArea.Size.Height / 2);
        }
    }

    private void UpdateResizeExpand(ResizeThumb resizeThumb, PointerEventArgs e)
    {
        if (resizeThumb is ResizeThumb.Left or ResizeThumb.Right)
            return;

        var pointerScreenPos = GetActualPointerPosition(e);
        var workArea = GetPointerGlobalWorkArea(pointerScreenPos);

        if (pointerScreenPos.Y < workArea.Y + BordersExpandThreshold ||
            pointerScreenPos.Y > workArea.Location.Y + workArea.Height - 1 - BordersExpandThreshold)
        {
            _expanded = true;
            _restoreSize = Size;
            Size = new Size(Size.Width, workArea.Size.Height);
            Location = new Point(Location.X, 0);
        }
    }

    private async Task OnHeaderPointerDownAsync(PointerEventArgs e)
    {
        if (e.Button == 0)
        {
            _movingProcess = true;
            _headerPointerOffset = new Point((int)e.OffsetX, (int)e.OffsetY);
            await JSRuntime.InvokeElementMethodAsync(headerDragArea, "setPointerCapture", e.PointerId);
            WindowMoveBegin?.Invoke();
        }
    }

    private void OnHeaderPointerUp(PointerEventArgs e)
    {
        if (_movingProcess)
        {
            _movingProcess = false;
            WindowMoveEnd?.Invoke();

            if (EnableExpand)
                UpdateMoveExpand(e);
        }
    }

    private void OnHeaderPointerMove(PointerEventArgs e)
    {
        if (_movingProcess)
        {
            if (_expanded)
            {
                var newPointerOffsetX = Size.Width - _headerPointerOffset.X < _restoreSize.Width ?
                    _headerPointerOffset.X - (Size.Width - _restoreSize.Width) :
                    _restoreSize.Width / 2;
                _restoreLocation = new Point(_headerPointerOffset.X - newPointerOffsetX, 0);
                _headerPointerOffset = new Point(newPointerOffsetX, _headerPointerOffset.Y);
                _expanded = false;

                Size = _restoreSize;
                if (Maximized)
                    Maximized = false; 
            }

            var pointerScreenPos = GetActualPointerPosition(e);
            var workArea = GetPointerGlobalWorkArea(pointerScreenPos);
            var limitedPointerPos = new Point(Math.Min(workArea.X + workArea.Width, Math.Max(workArea.X, pointerScreenPos.X)),
                                              Math.Min(workArea.Y + workArea.Height, Math.Max(workArea.Y, pointerScreenPos.Y)));

            Location = new Point(limitedPointerPos.X - _headerPointerOffset.X, limitedPointerPos.Y - _headerPointerOffset.Y);
            WindowMoving?.Invoke(e);
        }
    }

    private void OnHeaderDoubleClick()
    {
        if (MaximizeOnHeaderDoubleClick)
            Maximized = !Maximized;
    }

    private async Task OnResizeThumbPointerDown(ResizeThumb thumb, PointerEventArgs e)
    {
        if (e.Button == 0)
        {
            _movingProcess = true;
            _activeResizeThumb = thumb;
            _activeResizeThumbArea = _activeResizeThumb switch
            {
                ResizeThumb.Top => resizeThumbTop,
                ResizeThumb.Bottom => resizeThumbBottom,
                ResizeThumb.Left => resizeThumbLeft,
                ResizeThumb.Right => resizeThumbRight,
                ResizeThumb.TopLeft => resizeThumbTopLeft,
                ResizeThumb.TopRight => resizeThumbTopRight,
                ResizeThumb.BottomLeft => resizeThumbBottomLeft,
                ResizeThumb.BottomRight => resizeThumbBottomRight,
                _ => default,
            };
            await JSRuntime.InvokeElementMethodAsync(_activeResizeThumbArea, "setPointerCapture", e.PointerId);
        }
    }

    private void OnResizeThumbPointerUp(PointerEventArgs e)
    {
        if (_movingProcess)
        {
            _movingProcess = false;

            if (EnableExpand)
                UpdateResizeExpand(_activeResizeThumb, e);
        }
    }

    private void OnResizeThumbPointerMove(PointerEventArgs e)
    {
        if (_movingProcess)
        {
            var pointerScreenPos = GetActualPointerPosition(e);
            var workArea = GetPointerGlobalWorkArea(pointerScreenPos);
            var limitedPointerPos = new Point(Math.Min(workArea.X + workArea.Width, Math.Max(workArea.X, pointerScreenPos.X)),
                                              Math.Min(workArea.Y + workArea.Height, Math.Max(workArea.Y, pointerScreenPos.Y)));

            switch (_activeResizeThumb)
            {
                case ResizeThumb.Top:
                    var deltaTop = limitedPointerPos.Y - Location.Y;
                    deltaTop = Size.Height - Math.Max(MinSize.Height, Size.Height - deltaTop);
                    deltaTop = Size.Height - Math.Min(MaxSize.Height, Size.Height - deltaTop);
                    Location = new Point(Location.X, Location.Y + deltaTop);
                    Size = new Size(Size.Width, Size.Height - deltaTop);
                    break;
                case ResizeThumb.Bottom:
                    Size = new Size(Size.Width, limitedPointerPos.Y - Location.Y);
                    break;
                case ResizeThumb.Left:
                    var deltaLeft = limitedPointerPos.X - Location.X;
                    deltaLeft = Size.Width - Math.Max(MinSize.Width, Size.Width - deltaLeft);
                    deltaLeft = Size.Width - Math.Min(MaxSize.Width, Size.Width - deltaLeft);
                    Location = new Point(Location.X + deltaLeft, Location.Y);
                    Size = new Size(Size.Width - deltaLeft, Size.Height);
                    break;
                case ResizeThumb.Right:
                    Size = new Size(limitedPointerPos.X - Location.X, Size.Height);
                    break;
                case ResizeThumb.TopLeft:
                    var deltaTopLeft = new Point(limitedPointerPos.X - Location.X, limitedPointerPos.Y - Location.Y);
                    deltaTopLeft = new Point(Size.Width - Math.Max(MinSize.Width, Size.Width - deltaTopLeft.X),
                                             Size.Height - Math.Max(MinSize.Height, Size.Height - deltaTopLeft.Y));
                    deltaTopLeft = new Point(Size.Width - Math.Min(MaxSize.Width, Size.Width - deltaTopLeft.X),
                                             Size.Height - Math.Min(MaxSize.Height, Size.Height - deltaTopLeft.Y));
                    Location = new Point(Location.X + deltaTopLeft.X, Location.Y + deltaTopLeft.Y);
                    Size = new Size(Size.Width - deltaTopLeft.X, Size.Height - deltaTopLeft.Y);
                    break;
                case ResizeThumb.TopRight:
                    var deltaTopRight = limitedPointerPos.Y - Location.Y;
                    deltaTopRight = Size.Height - Math.Max(MinSize.Height, Size.Height - deltaTopRight);
                    deltaTopRight = Size.Height - Math.Min(MaxSize.Height, Size.Height - deltaTopRight);
                    Location = new Point(Location.X, Location.Y + deltaTopRight);
                    Size = new Size(limitedPointerPos.X - Location.X, Size.Height - deltaTopRight);
                    break;
                case ResizeThumb.BottomLeft:
                    var deltaBottomLeft = limitedPointerPos.X - Location.X;
                    deltaBottomLeft = Size.Width - Math.Max(MinSize.Width, Size.Width - deltaBottomLeft);
                    deltaBottomLeft = Size.Width - Math.Min(MaxSize.Width, Size.Width - deltaBottomLeft);
                    Location = new Point(Location.X + deltaBottomLeft, Location.Y);;
                    Size = new Size(Size.Width - deltaBottomLeft, limitedPointerPos.Y - Location.Y);
                    break;
                case ResizeThumb.BottomRight:
                    Size = new Size(limitedPointerPos.X - Location.X, limitedPointerPos.Y - Location.Y);
                    break;
            }
        }
    }

    #region Public methods
    /// <summary>
    /// Update information about monitors and its areas.
    /// </summary>
    public async Task UpdateScreensInfo()
    {
        _screensInfo = new();

        var screensInfo = await JSRuntime.InvokeAsync<JsonElement>("getScreensInfo");
        foreach(var s in screensInfo.EnumerateArray())
        {
            _screensInfo.Add(new ScreenInfo()
            {
                OriginalArea = new Rectangle(
                    (int)s[0].GetDouble(), (int)s[1].GetDouble(),
                    (int)s[2].GetDouble(), (int)s[3].GetDouble()
                ),
                ScaleFactor = s[4].GetDouble()
            });
        }

        var primaryScreen = _screensInfo.FirstOrDefault(s => s.OriginalArea.Location.IsEmpty);
        if (primaryScreen is null)
            throw new Exception("Unable to get correct screens info");

        if (_screensInfo.All(s => s.ScaleFactor == primaryScreen.ScaleFactor))
        {
            foreach (var s in _screensInfo)
            {
                s.ActualLeft = (int)(s.OriginalArea.Left * primaryScreen.ScaleFactor);
                s.ActualTop = (int)(s.OriginalArea.Top * primaryScreen.ScaleFactor);
                s.PositionActualized = true;
            }
        }
        else
        {
            var isHorisontalDirection =
                _screensInfo.Any(s1 => _screensInfo.Any(s2 => s2.OriginalArea.Left == s1.OriginalArea.Width)) ||
                _screensInfo.Any(s1 => s1.OriginalArea.Left == -s1.OriginalArea.Width);
            var isVerticalDirection =
                _screensInfo.Any(s1 => _screensInfo.Any(s2 => s2.OriginalArea.Top == s1.OriginalArea.Height)) ||
                _screensInfo.Any(s1 => s1.OriginalArea.Top == -s1.OriginalArea.Height);

            if (isHorisontalDirection !^ isVerticalDirection)
                throw new Exception("Only one-direction monitors positioning supported for different scale factors");
            else if (isHorisontalDirection)
            {
                var commonTopOffset = (int)(primaryScreen.OriginalArea.Height * (primaryScreen.ScaleFactor - 1));
                void updateActualPosition(LinkedListNode<ScreenInfo> screenNode)
                {
                    var screen = screenNode.Value;
                    if (screen.PositionActualized)
                        return;

                    if (screen.OriginalArea.Left == 0)
                    {
                        screen.ActualLeft = 0;
                    }
                    else if (screen.OriginalArea.Left > 0)
                    {
                        var screenOnLeft = screenNode.Previous.Value;
                        if (!screenOnLeft.PositionActualized)
                            updateActualPosition(screenNode.Previous);
                        screen.ActualLeft = screenOnLeft.ActualLeft + (int)(screenOnLeft.OriginalArea.Width * screenOnLeft.ScaleFactor);
                    }
                    else
                    {
                        var screenOnRight = screenNode.Next.Value;
                        if (!screenOnRight.PositionActualized)
                            updateActualPosition(screenNode.Next);
                        screen.ActualLeft = screenOnRight.ActualLeft - (int)(screen.OriginalArea.Width * screen.ScaleFactor);
                    }
                    screen.ActualTop = screen.OriginalArea.Top + commonTopOffset;
                    screen.PositionActualized = true;
                }

                var linkedScreensInfo = new LinkedList<ScreenInfo>(_screensInfo.OrderBy(s => s.OriginalArea.Left));
                var currentScreenNode = linkedScreensInfo.First;
                while(currentScreenNode != null)
                {
                    updateActualPosition(currentScreenNode);
                    currentScreenNode = currentScreenNode.Next;
                }
            }
            else if (isVerticalDirection)
            {
                var commonLeftOffset = (int)(primaryScreen.OriginalArea.Width * (primaryScreen.ScaleFactor - 1));
                void updateActualPosition(LinkedListNode<ScreenInfo> screenNode)
                {
                    var screen = screenNode.Value;
                    if (screen.PositionActualized)
                        return;

                    if (screen.OriginalArea.Top == 0)
                    {
                        screen.ActualTop = 0;
                    }
                    else if (screen.OriginalArea.Top > 0)
                    {
                        var screenOnTop = screenNode.Previous.Value;
                        if (!screenOnTop.PositionActualized)
                            updateActualPosition(screenNode.Previous);
                        screen.ActualTop = screenOnTop.ActualTop + (int)(screenOnTop.OriginalArea.Height * screenOnTop.ScaleFactor);
                    }
                    else
                    {
                        var screenOnBottom = screenNode.Next.Value;
                        if (!screenOnBottom.PositionActualized)
                            updateActualPosition(screenNode.Next);
                        screen.ActualTop = screenOnBottom.ActualTop - (int)(screen.OriginalArea.Height * screen.ScaleFactor);
                    }
                    screen.ActualLeft = screen.OriginalArea.Left + commonLeftOffset;
                    screen.PositionActualized = true;
                }

                var linkedScreensInfo = new LinkedList<ScreenInfo>(_screensInfo.OrderBy(s => s.OriginalArea.Top));
                var currentScreenNode = linkedScreensInfo.First;
                while (currentScreenNode != null)
                {
                    updateActualPosition(currentScreenNode);
                    currentScreenNode = currentScreenNode.Next;
                }
            }
        }
    }

    /// <summary>
    /// Minimize window.
    /// </summary>
    public void Minimize() => Minimized = true;

    /// <summary>
    /// Maximize window.
    /// </summary>
    public void Maximize() => Maximized = true;

    /// <summary>
    /// Restore window when maximized.
    /// </summary>
    public void Restore() => Maximized = false;

    /// <summary>
    /// Close window.
    /// </summary>
    public void Close() => Window.Close();

    /// <summary>
    /// Focus window.
    /// </summary>
    public void Focus()
    {
        // little hack
        Window.Topmost = true;
        Window.Topmost = false;
    }
    #endregion
}
