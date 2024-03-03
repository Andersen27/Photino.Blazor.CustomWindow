using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor.CustomWindow.Services;

namespace Photino.Blazor.CustomWindow.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomWindow(this IServiceCollection services)
    {
        return services.AddSingleton<ScreensAgentService>();
    }  
}