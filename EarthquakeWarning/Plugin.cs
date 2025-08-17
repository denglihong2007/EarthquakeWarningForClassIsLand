using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using EarthquakeWarning.Controls.NotificationProviders;
using EarthquakeWarning.Services;
using EarthquakeWarning.Services.NotificationProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EarthquakeWarning
{
    [PluginEntrance]
    public class Plugin : PluginBase
    {
        [STAThread]
        public override void Initialize(HostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<SharedService>();
            services.AddNotificationProvider<EarthquakeNotificationProvider, EarthquakeNotificationProviderSettingsControl>();
        }
    }
}

