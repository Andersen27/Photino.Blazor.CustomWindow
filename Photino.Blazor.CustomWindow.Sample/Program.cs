using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor.CustomWindow.Extensions;
using System;

namespace Photino.Blazor.CustomWindow.Sample
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            appBuilder.Services
                .AddCustomWindow()
                .AddLogging();

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