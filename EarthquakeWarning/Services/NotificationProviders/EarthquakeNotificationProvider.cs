using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.Net.Http;
using MaterialDesignThemes.Wpf;
using PluginWithNotificationProviders.Models;
using PluginWithNotificationProviders.Controls.NotificationProviders;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using EarthquakeWarning.Controls.NotificationProviders;
using EarthquakeWarning.Calculators;

namespace EarthquakeWarning.Services.NotificationProviders;

public class EarthquakeNotificationProvider : INotificationProvider, IHostedService
{
    public string Name { get; set; } = "地震预警";
    public string Description { get; set; } = "提供地震预警";
    public Guid ProviderGuid { get; set; } = new Guid("B27C0AF3-C917-44DE-A61D-8010C3F3FB92");

    private EarthquakeNotificationSettings Settings { get; }
    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; }
    private INotificationHostService NotificationHostService { get; }
    public EarthquakeReport EarthquakeReport { get; } = new();
    public LocalPosition LocalPosition { get; }

    public EarthquakeNotificationProvider(INotificationHostService notificationHostService)
    {
        IconElement = CreateIconElement();
        NotificationHostService = notificationHostService;
        NotificationHostService.RegisterNotificationProvider(this);
        Settings = NotificationHostService.GetNotificationProviderSettings<EarthquakeNotificationSettings>(ProviderGuid);
        SettingsElement = new EarthquakeNotificationProviderSettingsControl(Settings,this);
        LocalPosition = new LocalPosition { Latitude = Settings.Latitude, Longitude = Settings.Longitude };
        Task.Run(APIMonitor);
        EarthquakeReport.ReportUpdated += ReportUpdated;
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
        EarthquakeReport.UpdateFromJson("{\"ID\":7815,\"EventID\":\"20241109200514.0001_1\",\"ReportTime\": \"" + StartTime.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"ReportNum\": 1,\"OriginTime\": \"" + StartTime.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"HypoCenter\": \"四川省阿坝藏族羌族自治州汶川县\",\"Latitude\": 31.0,\"Longitude\": 103.4,\"Magunitude\": 4.8,\"Depth\": null,\"MaxIntensity\": 4.0\r\n}");
        await Task.Delay(10000);
        EarthquakeReport.UpdateFromJson("{\"ID\":7816,\"EventID\":\"20241109200514.0001_2\",\"ReportTime\": \"" + StartTime.AddSeconds(10).ToString("yyyy-MM-dd HH:mm:ss") + "\",\"ReportNum\": 2,\"OriginTime\": \"" + StartTime.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"HypoCenter\": \"四川省阿坝藏族羌族自治州汶川县\",\"Latitude\": 31.0,\"Longitude\": 103.4,\"Magunitude\": 6.8,\"Depth\": null,\"MaxIntensity\": 7.0\r\n}");
        await Task.Delay(10000);
        EarthquakeReport.UpdateFromJson("{\"ID\":7817,\"EventID\":\"20241109200514.0001_3\",\"ReportTime\": \"" + StartTime.AddSeconds(20).ToString("yyyy-MM-dd HH:mm:ss") + "\",\"ReportNum\": 3,\"OriginTime\": \"" + StartTime.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"HypoCenter\": \"四川省阿坝藏族羌族自治州汶川县\",\"Latitude\": 31.0,\"Longitude\": 103.4,\"Magunitude\": 7.8,\"Depth\": null,\"MaxIntensity\": 9.0\r\n}");
        await Task.Delay(10000);
        EarthquakeReport.UpdateFromJson("{\"ID\":7818,\"EventID\":\"20241109200514.0001_4\",\"ReportTime\": \"" + StartTime.AddSeconds(30).ToString("yyyy-MM-dd HH:mm:ss") + "\",\"ReportNum\": 4,\"OriginTime\": \"" + StartTime.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"HypoCenter\": \"四川省阿坝藏族羌族自治州汶川县\",\"Latitude\": 31.0,\"Longitude\": 103.4,\"Magunitude\": 8.0,\"Depth\": null,\"MaxIntensity\": 11.0\r\n}");
    }
    private bool _firstload = true;
    private void ReportUpdated(EarthquakeReport obj)
    {
        Settings.Info = $"在{obj.ReportTime}时预警第{obj.ReportNum}报发出：在{obj.OriginTime}时，{obj.HypoCenter}({obj.Latitude} {obj.Longitude})发生{obj.Magunitude}级地震，最大烈度{obj.MaxIntensity}度。";
        if (_firstload) 
        {
            _firstload = false;
            return;
        }
        if (LocalIntensityCalculator.CalculateLocalIntensity(EarthquakeReport, LocalPosition) > Settings.Threshold)
        {
            if (!_showing)
            {
                double expectTime = SWaveArrivalTimeCalculater.CalculateSWaveArrivalTime(EarthquakeReport, LocalPosition);
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
            MaskContent = new EarthquakeNotificationProviderControl("EarthquakeNotifyMask", EarthquakeReport, LocalPosition),
            MaskDuration = TimeSpan.FromSeconds(3),
            OverlayContent = new EarthquakeNotificationProviderControl("EarthquakeNotifyOverlay", EarthquakeReport, LocalPosition),
            OverlayDuration = DateTime.Parse(EarthquakeReport.OriginTime).AddSeconds(expectTime) - DateTime.Now.AddSeconds(3)
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
                string response = await client.GetStringAsync("https://api.wolfx.jp/sc_eew.json");
                if (!_testing)
                {
                    EarthquakeReport.UpdateFromJson(response);
                }
            }
            catch
            {

            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}

public class EarthquakeReport : ObservableRecipient
{
    // 你的属性定义
    public int ID { get; set; }
    public string EventID { get; set; } = "";
    public string ReportTime { get; set; }
    public int ReportNum { get; set; }
    public string OriginTime { get; set; }
    public string HypoCenter { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Magunitude { get; set; }
    public double? Depth { get; set; }  // 使用 nullable 类型以处理可能为 null 的值
    public double MaxIntensity { get; set; }

    // 定义事件
    public event Action<EarthquakeReport> ReportUpdated;

    // 更新整个报告的方法
    public void UpdateFromJson(string jsonResponse)
    {
        var updatedReport = JsonSerializer.Deserialize<EarthquakeReport>(jsonResponse);
        if (updatedReport != null)
        {
            ID = updatedReport.ID;
            EventID = updatedReport.EventID;
            ReportTime = updatedReport.ReportTime;
            ReportNum = updatedReport.ReportNum;
            OriginTime = updatedReport.OriginTime;
            HypoCenter = updatedReport.HypoCenter;
            Latitude = updatedReport.Latitude;
            Longitude = updatedReport.Longitude;
            Magunitude = updatedReport.Magunitude;
            Depth = updatedReport.Depth;
            MaxIntensity = updatedReport.MaxIntensity;
            OnReportUpdated();
        }
    }

    // 触发事件的方法
    protected virtual void OnReportUpdated()
    {
        ReportUpdated?.Invoke(this);
    }
}
public class LocalPosition
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}