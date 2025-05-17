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
using EarthquakeWarning.Models.EarthquakeModels;
using System.Diagnostics;
using System.Text.Json;
using EarthquakeWarning.Models;

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
    public LocalPosition LocalPosition { get; }
    public EarthquakeInfo EarthquakeInfo { get; } = new();
    private HuaniaEarthQuakeCalculator huaniaEarthQuakeCalculator = new();
    public EarthquakeNotificationProvider(INotificationHostService notificationHostService)
    {
        IconElement = CreateIconElement();
        NotificationHostService = notificationHostService;
        NotificationHostService.RegisterNotificationProvider(this);
        Settings = NotificationHostService.GetNotificationProviderSettings<EarthquakeNotificationSettings>(ProviderGuid);
        SettingsElement = new EarthquakeNotificationProviderSettingsControl(Settings, this);
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
        for (int i = 0; i < 6; i++)
        {
            EarthquakeInfo.UpdateInfo(new EarthquakeInfo
            {
                ID = 0,
                EventID = (eventId + i).ToString(),
                ReportTime = DateTime.Now,
                ReportNum = i,
                OriginTime = StartTime,
                HypoCenter = "四川省阿坝藏族羌族自治州汶川县",
                Latitude = 31.0,
                Longitude = 103.4,
                Depth = 14.0,
                MaxIntensity = 12,
                Magunitude = 4.0 + (i + 1.0) * 0.8,
                Pond = "43",
            });
            await Task.Delay(10000);
        }
        _testing = false;
    }
    private bool _firstload = true;
    private void Update(EarthquakeInfo obj)
    {
        Settings.Info = $"在{obj.OriginTime:G}时，{obj.HypoCenter}({obj.Latitude} {obj.Longitude})发生{obj.Magunitude}级地震，震源深度{obj.Depth}km。本地距离{huaniaEarthQuakeCalculator.GetDistance(LocalPosition.Latitude, LocalPosition.Longitude, obj.Latitude, obj.Longitude):F0}km，本地烈度{huaniaEarthQuakeCalculator.GetIntensity(obj.Magunitude, huaniaEarthQuakeCalculator.GetDistance(LocalPosition.Latitude, LocalPosition.Longitude, obj.Latitude, obj.Longitude)):F1}。";
        if (_firstload)
        {
            _firstload = false;
            return;
        }
        double distance = huaniaEarthQuakeCalculator.GetDistance(LocalPosition.Latitude, LocalPosition.Longitude, obj.Latitude, obj.Longitude);
        if (huaniaEarthQuakeCalculator.GetIntensity(obj.Magunitude, distance) > Settings.Threshold)
        {
            if (!_showing)
            {
                double expectTime = huaniaEarthQuakeCalculator.GetCountDownSeconds(obj.Depth??17.4,distance);
                DateTime pWaveArriveTime = obj.OriginTime.AddSeconds(expectTime);
                if (DateTime.Now > pWaveArriveTime)
                {
                    return;
                }
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
            OverlayDuration = EarthquakeInfo.OriginTime.AddSeconds(expectTime) - DateTime.Now.AddSeconds(3)
        };
        await NotificationHostService.ShowNotificationAsync(notice);
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
                    EarthquakeInfo.UpdateInfo(await GetEarthQuake());
                }
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
            }
        }
    }
    public static async Task<EarthquakeInfo> GetEarthQuake()
    {
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync("https://api.wolfx.jp/sc_eew.json").ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateTimeConverter());
        return await JsonSerializer.DeserializeAsync<EarthquakeInfo>(stream,options);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}