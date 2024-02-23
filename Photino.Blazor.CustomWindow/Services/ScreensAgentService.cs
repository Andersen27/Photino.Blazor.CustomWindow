using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Drawing;
using System.Text.Json;

namespace Photino.Blazor.CustomWindow.Services;

public class ScreensAgentService()
{
    private IJSObjectReference module;

    private List<ScreenInfo> screensInfo;

    public bool Initialized => screensInfo != null;

    public Point GetOSPointerPosition(PointerEventArgs e)
    {
        var pointerScreenPos = new Point((int)e.ScreenX, (int)e.ScreenY);
        var screen = screensInfo.First(s => s.OriginalArea.Contains(pointerScreenPos));
        pointerScreenPos.Offset(-screen.OriginalArea.Left, -screen.OriginalArea.Top);
        return new Point(screen.ActualLeft + (int)(pointerScreenPos.X * screen.ScaleFactor),
                         screen.ActualTop + (int)(pointerScreenPos.Y * screen.ScaleFactor));
    }

    public double GetPointerPositionScaleFactor(PointerEventArgs e)
    {
        var pointerScreenPos = new Point((int)e.ScreenX, (int)e.ScreenY);
        var screen = screensInfo.First(s => s.OriginalArea.Contains(pointerScreenPos));
        return screen.ScaleFactor;
    }

    public async Task Initialize(IJSRuntime jsRuntime)
    {
        if (!Initialized)
        {
            module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Photino.Blazor.CustomWindow/js/pb-service-agent.js");
            await UpdateScreensInfo();
        }
    }

    public async Task UpdateScreensInfo()
    {
        this.screensInfo = [];

        var screensInfo = await module.InvokeAsync<JsonElement[]>("getScreensInfo");
        foreach (var screen in screensInfo)
        {
            this.screensInfo.Add(new ScreenInfo()
            {
                OriginalArea = new Rectangle(
                    (int)screen[0].GetDouble(), (int)screen[1].GetDouble(),
                    (int)screen[2].GetDouble(), (int)screen[3].GetDouble()
                ),
                ScaleFactor = screen[4].GetDouble()
            });
        }

        var primaryScreen = this.screensInfo.FirstOrDefault(s => s.OriginalArea.Location.IsEmpty)
            ?? throw new Exception("Unable to get correct screens info");

        primaryScreen.PositionActualized = true;
        if (this.screensInfo.Count == 1)
        {
            return;
        }
        else if (this.screensInfo.All(s => s.ScaleFactor == 1))
        {
            foreach (var s in this.screensInfo)
            {
                s.ActualLeft = s.OriginalArea.Left;
                s.ActualTop = s.OriginalArea.Top;
                s.PositionActualized = true;
            }
        }
        else
        {
            var isHorisontalDirection =
                this.screensInfo.Any(s1 => this.screensInfo.Any(s2 => s2 != s1 && s2.OriginalArea.Left >= s1.OriginalArea.Width)) ||
                this.screensInfo.Any(s1 => s1.OriginalArea.Left <= -s1.OriginalArea.Width);
            var isVerticalDirection =
                this.screensInfo.Any(s1 => this.screensInfo.Any(s2 => s2 != s1 && s2.OriginalArea.Top >= s1.OriginalArea.Height)) ||
                this.screensInfo.Any(s1 => s1.OriginalArea.Top <= -s1.OriginalArea.Height);

            if (!(isHorisontalDirection ^ isVerticalDirection))
            {
                throw new Exception("Only one-direction monitors positioning supported for different scale factors");
            }
            else if (isHorisontalDirection)
            {
                var commonTopOffset = (int)(primaryScreen.OriginalArea.Height * (primaryScreen.ScaleFactor - 1));

                var linkedScreensInfo = new LinkedList<ScreenInfo>(this.screensInfo.OrderBy(s => s.OriginalArea.Left));
                var currentScreenNode = linkedScreensInfo.First;
                while (currentScreenNode != null)
                {
                    UpdateActualHorizontalPosition(currentScreenNode, commonTopOffset);
                    currentScreenNode = currentScreenNode.Next;
                }
            }
            else if (isVerticalDirection)
            {
                var commonLeftOffset = (int)(primaryScreen.OriginalArea.Width * (primaryScreen.ScaleFactor - 1));

                var linkedScreensInfo = new LinkedList<ScreenInfo>(this.screensInfo.OrderBy(s => s.OriginalArea.Top));
                var currentScreenNode = linkedScreensInfo.First;
                while (currentScreenNode != null)
                {
                    UpdateActualVerticalPosition(currentScreenNode, commonLeftOffset);
                    currentScreenNode = currentScreenNode.Next;
                }
            }
        }
    }

    private static void UpdateActualHorizontalPosition(LinkedListNode<ScreenInfo> screenNode, int commonTopOffset)
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
                UpdateActualHorizontalPosition(screenNode.Previous, commonTopOffset);
            screen.ActualLeft = screenOnLeft.ActualLeft + (int)(screenOnLeft.OriginalArea.Width * screenOnLeft.ScaleFactor);
        }
        else
        {
            var screenOnRight = screenNode.Next.Value;
            if (!screenOnRight.PositionActualized)
                UpdateActualHorizontalPosition(screenNode.Next, commonTopOffset);
            screen.ActualLeft = screenOnRight.ActualLeft - (int)(screen.OriginalArea.Width * screen.ScaleFactor);
        }
        screen.ActualTop = screen.OriginalArea.Top + commonTopOffset;
        screen.PositionActualized = true;
    }

    private static void UpdateActualVerticalPosition(LinkedListNode<ScreenInfo> screenNode, int commonLeftOffset)
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
                UpdateActualVerticalPosition(screenNode.Previous, commonLeftOffset);
            screen.ActualTop = screenOnTop.ActualTop + (int)(screenOnTop.OriginalArea.Height * screenOnTop.ScaleFactor);
        }
        else
        {
            var screenOnBottom = screenNode.Next.Value;
            if (!screenOnBottom.PositionActualized)
                UpdateActualVerticalPosition(screenNode.Next, commonLeftOffset);
            screen.ActualTop = screenOnBottom.ActualTop - (int)(screen.OriginalArea.Height * screen.ScaleFactor);
        }
        screen.ActualLeft = screen.OriginalArea.Left + commonLeftOffset;
        screen.PositionActualized = true;
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