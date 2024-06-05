using Microsoft.AspNetCore.Components.Web;
using System.Drawing;
using Monitor = Photino.NET.Monitor;

namespace Photino.Blazor.CustomWindow.Services;

public class ScreensAgentService(PhotinoBlazorApp photinoBlazorApp)
{
    private PhotinoBlazorApp PhotinoBlazorApp { get; set; } = photinoBlazorApp;

    private Dictionary<Monitor, Rectangle> _monitorsWebScreens;

    public bool Inited => _monitorsWebScreens != null;

    private static Rectangle ScaleRect(Rectangle rect, double scale)
    {
        return new Rectangle(rect.X, rect.Y, (int)(rect.Width * scale), (int)(rect.Height * scale));
    }

    public Point GetOSPointerPosition(MouseEventArgs e)
    {
        var pointerScreenPos = new Point((int)e.ScreenX, (int)e.ScreenY);
        var monitorScreenPair = _monitorsWebScreens.First(s => s.Value.Contains(pointerScreenPos));
        var monitor = monitorScreenPair.Key;
        var webScreen = monitorScreenPair.Value;
        return new()
        {
            X = (int)(monitor.MonitorArea.X + monitor.Scale * (pointerScreenPos.X - webScreen.X)),
            Y = (int)(monitor.MonitorArea.Y + monitor.Scale * (pointerScreenPos.Y - webScreen.Y)),
        };
    }

    public double GetPointerScreenScale(MouseEventArgs e)
    {
        var pointerScreenPos = new Point((int)e.ScreenX, (int)e.ScreenY);
        var monitor = _monitorsWebScreens.First(s => s.Value.Contains(pointerScreenPos)).Key;
        return monitor.Scale;
    }

    public void InitializeIfNeed()
    {
        if (!Inited)
            UpdateScreensInfo();
    }

    public void UpdateScreensInfo()
    {
        // init monitors and primary monitor
        var monitors = PhotinoBlazorApp.MainWindow.Monitors.ToArray();
        var primaryMonitor = monitors.Single(m => m.MonitorArea.Location.IsEmpty);

        // simple calculation if there is single monitor or no specific scale factors
        if (monitors.Length == 1 || monitors.All(m => m.Scale == 1))
        {
            _monitorsWebScreens = new();
            foreach (var monitor in monitors)
                _monitorsWebScreens[monitor] = ScaleRect(monitor.MonitorArea, 1 / monitor.Scale);
            return;
        }

        // determine monitors positioning direction
        var isHorisontalDirection =
            monitors.Any(m1 => monitors.Except([m1]).Any(m2 => m2.MonitorArea.Left >= m1.MonitorArea.Width)) ||
            monitors.Any(m1 => m1.MonitorArea.Left <= -m1.MonitorArea.Width);
        var isVerticalDirection =
            monitors.Any(m1 => monitors.Except([m1]).Any(m2 => m2.MonitorArea.Top >= m1.MonitorArea.Height)) ||
            monitors.Any(m1 => m1.MonitorArea.Top <= -m1.MonitorArea.Height);

        if (!(isHorisontalDirection ^ isVerticalDirection))
        {
            throw new Exception("Only one-direction monitors positioning supported for different scale factors");
        }
        else
        {
            // add primary monitor to dictionary
            var primaryWebScreen = ScaleRect(primaryMonitor.MonitorArea, 1 / primaryMonitor.Scale);
            _monitorsWebScreens = new() { {primaryMonitor, primaryWebScreen} };
            Rectangle lastWebScreen;

            // horizontal direction calculation
            if (isHorisontalDirection)
            {
                var monitorsOrderedByX = monitors.OrderBy(m => m.MonitorArea.X).ToArray();
                var primaryMonitorIndex = Array.IndexOf(monitorsOrderedByX, primaryMonitor);

                lastWebScreen = primaryWebScreen;
                for (int i = primaryMonitorIndex + 1; i < monitorsOrderedByX.Length; i++)
                {
                    var monitor = monitorsOrderedByX[i];
                    var webScreen = ScaleRect(monitor.MonitorArea, 1 / monitor.Scale);
                    webScreen.X = lastWebScreen.Right;
                    webScreen.Y = (int)(monitor.MonitorArea.Y / primaryMonitor.Scale) +
                        (monitor.MonitorArea.Y > 0 ? 0 : primaryWebScreen.Bottom - webScreen.Height);
                    _monitorsWebScreens[monitor] = webScreen;
                    lastWebScreen = webScreen;
                }

                lastWebScreen = primaryWebScreen;
                for (int i = primaryMonitorIndex - 1; i >= 0; i--)
                {
                    var monitor = monitorsOrderedByX[i];
                    var webScreen = ScaleRect(monitor.MonitorArea, 1 / monitor.Scale);
                    webScreen.X = lastWebScreen.Left - webScreen.Width;
                    webScreen.Y = (int)(monitor.MonitorArea.Y / primaryMonitor.Scale) +
                        (monitor.MonitorArea.Y > 0 ? 0 : primaryWebScreen.Bottom - webScreen.Height);
                    _monitorsWebScreens[monitor] = webScreen;
                    lastWebScreen = webScreen;
                }
            }

            // vertical direction calculation
            if (isVerticalDirection)
            {
                var monitorsOrderedByY = monitors.OrderBy(m => m.MonitorArea.Y).ToArray();
                var primaryMonitorIndex = Array.IndexOf(monitorsOrderedByY, primaryMonitor);

                lastWebScreen = primaryWebScreen;
                for (int i = primaryMonitorIndex + 1; i < monitorsOrderedByY.Length; i++)
                {
                    var monitor = monitorsOrderedByY[i];
                    var webScreen = ScaleRect(monitor.MonitorArea, 1 / monitor.Scale);
                    webScreen.Y = lastWebScreen.Bottom;
                    webScreen.X = (int)(monitor.MonitorArea.X / primaryMonitor.Scale) +
                        (monitor.MonitorArea.X > 0 ? 0 : primaryWebScreen.Right - webScreen.Width);
                    _monitorsWebScreens[monitor] = webScreen;
                    lastWebScreen = webScreen;
                }

                lastWebScreen = primaryWebScreen;
                for (int i = primaryMonitorIndex - 1; i >= 0; i--)
                {
                    var monitor = monitorsOrderedByY[i];
                    var webScreen = ScaleRect(monitor.MonitorArea, 1 / monitor.Scale);
                    webScreen.Y = lastWebScreen.Top - webScreen.Height;
                    webScreen.X = (int)(monitor.MonitorArea.X / primaryMonitor.Scale) +
                        (monitor.MonitorArea.X > 0 ? 0 : primaryWebScreen.Right - webScreen.Width);
                    _monitorsWebScreens[monitor] = webScreen;
                    lastWebScreen = webScreen;
                }
            }
        }
    }
}
