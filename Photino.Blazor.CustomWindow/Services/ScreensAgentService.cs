using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Drawing;
using System.Text.Json;

namespace Photino.Blazor.CustomWindow.Services;

public class ScreensAgentService()
{
    private IJSObjectReference _module;

    private List<ScreenInfo> _screensInfo;

    public bool Initialized => _screensInfo != null;

    public Point GetOSPointerPosition(PointerEventArgs e)
    {
        var pointerScreenPos = new Point((int)e.ScreenX, (int)e.ScreenY);
        var screen = _screensInfo.First(s => s.OriginalArea.Contains(pointerScreenPos));
        pointerScreenPos.Offset(-screen.OriginalArea.Left, -screen.OriginalArea.Top);
        return new Point(screen.ActualLeft + (int)(pointerScreenPos.X * screen.ScaleFactor),
                         screen.ActualTop + (int)(pointerScreenPos.Y * screen.ScaleFactor));
    }

    public double GetPointerPositionScaleFactor(PointerEventArgs e)
    {
        var pointerScreenPos = new Point((int)e.ScreenX, (int)e.ScreenY);
        var screen = _screensInfo.First(s => s.OriginalArea.Contains(pointerScreenPos));
        return screen.ScaleFactor;
    }

    public async Task Initialize(IJSRuntime jsRuntime)
    {
        if (!Initialized)
        {
            _module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Photino.Blazor.CustomWindow/js/pb-service-agent.js");
            await UpdateScreensInfo();
        }
    }

    public async Task UpdateScreensInfo()
    {
        _screensInfo = [];

        var screensInfo = await _module.InvokeAsync<JsonElement[]>("getScreensInfo");
        foreach (var s in screensInfo)
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

        var primaryScreen = _screensInfo.FirstOrDefault(s => s.OriginalArea.Location.IsEmpty)
            ?? throw new Exception("Unable to get correct screens info");

        primaryScreen.PositionActualized = true;
        if (_screensInfo.Count == 1)
        {
            return;
        }
        else if (_screensInfo.All(s => s.ScaleFactor == 1))
        {
            foreach (var s in _screensInfo)
            {
                s.ActualLeft = s.OriginalArea.Left;
                s.ActualTop = s.OriginalArea.Top;
                s.PositionActualized = true;
            }
        }
        else
        {
            var isHorisontalDirection =
                _screensInfo.Any(s1 => _screensInfo.Any(s2 => s2 != s1 && s2.OriginalArea.Left >= s1.OriginalArea.Width)) ||
                _screensInfo.Any(s1 => s1.OriginalArea.Left <= -s1.OriginalArea.Width);
            var isVerticalDirection =
                _screensInfo.Any(s1 => _screensInfo.Any(s2 => s2 != s1 && s2.OriginalArea.Top >= s1.OriginalArea.Height)) ||
                _screensInfo.Any(s1 => s1.OriginalArea.Top <= -s1.OriginalArea.Height);

            if (!(isHorisontalDirection ^ isVerticalDirection))
            {
                throw new Exception("Only one-direction monitors positioning supported for different scale factors");
            }
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
                while (currentScreenNode != null)
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

    private sealed class ScreenInfo
    {
        public int ActualLeft { get; set; }
        public int ActualTop { get; set; }
        public Rectangle OriginalArea { get; set; }
        public bool PositionActualized { get; set; } = false;
        public double ScaleFactor { get; set; }
    }
}