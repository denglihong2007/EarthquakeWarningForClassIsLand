using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.Net.Http;
using MaterialDesignThemes.Wpf;
using PluginWithNotificationProviders.Models;
using PluginWithNotificationProviders.Controls.NotificationProviders;
using EarthquakeWarning.Controls.NotificationProviders;
using System.Text;
using EarthquakeWarning.Models.EarthquakeModels;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace EarthquakeWarning.Services.NotificationProviders;

public class EarthquakeNotificationProvider : INotificationProvider, IHostedService
{
    public string Name { get; set; } = "地震预警";
    public string Description { get; set; } = "在地震发生时，根据用户设置发出地震预警。";
    public Guid ProviderGuid { get; set; } = new Guid("B27C0AF3-C917-44DE-A61D-8010C3F3FB92");

    private EarthquakeNotificationSettings Settings { get; }
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; }
    private INotificationHostService NotificationHostService { get; }
    public EarthquakeInfoBase EarthquakeInfo = new();
    public LocalPosition LocalPosition { get; }
    private HuaniaEarthQuakeCalculator huaniaEarthQuakeCalculator = new();
    public EarthquakeNotificationProvider(INotificationHostService notificationHostService)
    {
        IconElement = CreateIconElement();
        NotificationHostService = notificationHostService;
        NotificationHostService.RegisterNotificationProvider(this);
        Settings = NotificationHostService.GetNotificationProviderSettings<EarthquakeNotificationSettings>(ProviderGuid);
        SettingsElement = new EarthquakeNotificationProviderSettingsControl(Settings,this);
        LocalPosition = new LocalPosition { Latitude = Settings.Latitude, Longitude = Settings.Longitude };
        Task.Run(APIMonitor);
        EarthquakeInfo.ReportUpdated += Update;
    }

    private static PackIcon CreateIconElement()
    {
        return new PackIcon
        {
            Kind = PackIconKind.ShieldHome,
            Width = 24,
            Height = 24
        };
    }
    private bool _showing = false;
    private bool _testing = false;

    public async Task Example() 
    {
        _testing = true;
        DateTime StartTime = DateTime.Now;
        int eventId = 0; 
        for (int i = 0;i<6;i++)
        {
            EarthquakeInfo.UpdateInfo(new EarthquakeInfoBase
            {
                Id = (eventId + i).ToString(),
                StartAt = StartTime,
                UpdateAt = DateTime.Now,
                Latitude = 31.0,
                Longitude = 103.4,
                Magnitude = 4.0 + (i + 1.0) * 0.8,
                Depth = 14.0,
                PlaceName = "四川省阿坝藏族羌族自治州汶川县"
            });
            await Task.Delay(10000);

        }
    }
    private bool _firstload = true;
    private void Update(EarthquakeInfoBase obj)
    {
        Settings.Info = $"在{obj.StartAt:G}时，{obj.PlaceName}({obj.Latitude} {obj.Longitude})发生{obj.Magnitude}级地震，震源深度{obj.Depth}km。本地距离{huaniaEarthQuakeCalculator.GetDistance(LocalPosition.Latitude, LocalPosition.Longitude, obj.Latitude, obj.Longitude):F0}km，本地烈度{huaniaEarthQuakeCalculator.GetIntensity(obj.Magnitude, huaniaEarthQuakeCalculator.GetDistance(LocalPosition.Latitude, LocalPosition.Longitude, obj.Latitude, obj.Longitude)):F1}。";
        if (_firstload) 
        {
            _firstload = false;
            return;
        }
        double distance = huaniaEarthQuakeCalculator.GetDistance(LocalPosition.Latitude, LocalPosition.Longitude, obj.Latitude, obj.Longitude);
        if (huaniaEarthQuakeCalculator.GetIntensity(obj.Magnitude, distance) > Settings.Threshold)
        {
            if (!_showing)
            {
                double expectTime = huaniaEarthQuakeCalculator.GetCountDownSeconds(obj.Depth, distance);
                Application.Current.Dispatcher.BeginInvoke(() => ShowNotificationAsync(expectTime));
                _showing = true; // 在定时器启动之前设置为 true

                var timer = new System.Timers.Timer(expectTime * 1000);
                timer.Elapsed += (s, e) =>
                {
                    _showing = false; // 在释放锁之后才能设置为 false
                    timer.Stop(); // 停止定时器
                    timer.Dispose(); // 释放资源
                };
                timer.Start();
            }
        }
    }

    private async Task ShowNotificationAsync(double expectTime)
    {
        var notice = new NotificationRequest
        {
            MaskContent = new EarthquakeNotificationProviderControl("EarthquakeNotifyMask", EarthquakeInfo, LocalPosition),
            MaskDuration = TimeSpan.FromSeconds(3),
            OverlayContent = new EarthquakeNotificationProviderControl("EarthquakeNotifyOverlay", EarthquakeInfo, LocalPosition),
            OverlayDuration = EarthquakeInfo.StartAt.AddSeconds(expectTime) - DateTime.Now.AddSeconds(3)
        };
        await NotificationHostService.ShowNotificationAsync(notice);
        if (_testing) 
        {
            _testing = false;
        }
    }

    public async void APIMonitor()
    {
        using var client = new HttpClient();
        while (true)
        {
            try
            {
                await Task.Delay(1000);
                if (!_testing)
                {
                    EarthquakeInfo.UpdateInfo(GetEarthQuakeList().Result[0]);
                }
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
            }
        }
    }
    private string HuaniaApi = Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9tb2JpbGUtbmV3LmNoaW5hZWV3LmNuL3YxLw=="));
    private readonly IHttpRequester _httpRequester = new HttpRequester();
    private readonly IJsonConvertService _jsonConvert = new JsonConvertService();
    public async Task<List<EarthquakeInfoBase>> GetEarthQuakeList()
    {
        var response = _jsonConvert.ConvertTo<HuaniaWarningsResponse>(
            await _httpRequester.GetString(HuaniaApi + "earlywarnings?updates=3&start_at=0", null).ConfigureAwait(false));
        if (response?.Code != 0)
            throw new Exception(response?.Message);
        return response.Data.Select(t => t.MapToEarthQuakeInfo()).ToList();
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
public class HuaniaEarthQuakeDto
{
    [JsonPropertyName("eventId")] public long EventId { get; set; }

    [JsonPropertyName("updates")] public long Updates { get; set; }

    [JsonPropertyName("latitude")] public double Latitude { get; set; }

    [JsonPropertyName("longitude")] public double Longitude { get; set; }

    [JsonPropertyName("depth")] public double Depth { get; set; }

    [JsonPropertyName("epicenter")] public string Epicenter { get; set; }

    [JsonPropertyName("startAt")] public DateTime StartAt { get; set; }

    [JsonPropertyName("updateAt")] public DateTime UpdateAt { get; set; }

    [JsonPropertyName("magnitude")] public double Magnitude { get; set; }

    [JsonPropertyName("insideNet")] public long InsideNet { get; set; }

    [JsonPropertyName("sations")] public long Sations { get; set; }
}

public class HuaniaEarthQuakeInfoResponse
{
    [JsonPropertyName("code")] public long Code { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("data")] public List<HuaniaEarthQuakeDto> Data { get; set; }
}

public class HuaniaWarningsResponse
{
    [JsonPropertyName("code")] public long Code { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; }

    [JsonPropertyName("data")] public List<HuaniaEarthQuakeDto> Data { get; set; }
}

public static class HuaniaEarthQuakeToEarthQuakeInfoMapper
{
    public static EarthquakeInfoBase MapToEarthQuakeInfo(this HuaniaEarthQuakeDto earthQuake)
    {
        return new EarthquakeInfoBase
        {
            Id = earthQuake.EventId.ToString(),
            StartAt = earthQuake.StartAt,
            UpdateAt = earthQuake.UpdateAt,
            Latitude = earthQuake.Latitude,
            Longitude = earthQuake.Longitude,
            Magnitude = earthQuake.Magnitude,
            Depth = earthQuake.Depth,
            PlaceName = earthQuake.Epicenter
        };
    }
}
public class LocalPosition
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}