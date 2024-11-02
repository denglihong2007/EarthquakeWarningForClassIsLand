
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
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
            services.AddHostedService<EarthquakeNotificationProvider>();
        }
    }
}

