using System;
using Photino.Blazor.CustomWindow.Extensions;

namespace Photino.Blazor.CustomWindow.Sample
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            // add custom window services
            appBuilder.Services.AddCustomWindow();

            // register root component and selector
            appBuilder.RootComponents.Add<App>("app");

            var app = appBuilder.Build();

            // customize window
            app.MainWindow
                .SetChromeless(true) // necessarily for custom window
                .SetIconFile("favicon.ico")
                .SetTitle("Photino Blazor Sample");

            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
            };

            app.Run();

        }
    }
}
