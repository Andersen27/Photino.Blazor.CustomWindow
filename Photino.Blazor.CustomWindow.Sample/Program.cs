﻿using System;
using Microsoft.Extensions.DependencyInjection;

namespace Photino.Blazor.CustomWindow.Sample
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            appBuilder.Services
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
