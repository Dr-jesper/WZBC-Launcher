using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.Conduit;

namespace PCL;

public partial class ClubLaunchPanel : UserControl
{
    private bool _ready;
    private ServerChoice[] _serverChoices = [];
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private readonly ClubServerStatusProbe _statusProbe = new();
    private CancellationTokenSource? _statusCancellation;
    private int _statusGeneration;
    private bool _statusRefreshRunning;

    public sealed class ServerChoice(ClubServer server, bool selected) : INotifyPropertyChanged
    {
        public string Id => server.Id;
        public string Name => server.Name;
        public string Detail => server.Id == "none" ? "不自动连接服务器" : server.Address + " · 版本与准入请见入服指南";
        public bool Selected
        {
            get => selected;
            set
            {
                if (selected == value) return;
                selected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public ClubLaunchPanel()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            var selected = ClubCatalog.FindServer(Config.System.ClubServer);
            _serverChoices = ClubCatalog.Servers.Select(s => new ServerChoice(s, s.Id == selected.Id)).ToArray();
            Servers.ItemsSource = _serverChoices;
            UpdateDestination(selected);
            _ready = true;
            StartStatusRefresh();
        };
        _statusTimer.Tick += (_, _) =>
        {
            if (_statusCancellation is { } cancellation)
                _ = RefreshServerStatusesAsync(_statusGeneration, cancellation.Token);
        };
        Unloaded += (_, _) =>
        {
            _ready = false;
            StopStatusRefresh();
        };
    }

    private void Server_Checked(object sender, RoutedEventArgs e)
    {
        if (!_ready || sender is not RadioButton { Tag: string id }) return;
        var server = ClubCatalog.FindServer(id);
        foreach (var choice in _serverChoices) choice.Selected = choice.Id == server.Id;
        Config.System.ClubServer = server.Id;
        UpdateDestination(server);
        ClubOnlineCount.Text = server.Id == "none" ? "—" : "未知";
        ClubOnlineBadge.ToolTip = server.Id == "none"
            ? "选择服务器后显示该入口的在线数据；两条模组服线路不重复计数。"
            : "正在读取所选入口；服务器状态不代表账户拥有准入资格。";
        StartStatusRefresh();
    }

    private void UpdateDestination(ClubServer server) => DestinationHint.Text = server.Id == "none"
        ? "当前选择：只启动游戏，不自动入服。"
        : $"启动后前往 {server.Name}。请使用对应实例；版本与准入请向管理员确认。";

    private void StartStatusRefresh()
    {
        StopStatusRefresh();
        _statusCancellation = new CancellationTokenSource();
        var generation = ++_statusGeneration;
        _statusTimer.Start();
        _ = RefreshServerStatusesAsync(generation, _statusCancellation.Token);
    }

    private void StopStatusRefresh()
    {
        _statusTimer.Stop();
        ++_statusGeneration;
        _statusCancellation?.Cancel();
        _statusCancellation?.Dispose();
        _statusCancellation = null;
    }

    private async Task RefreshServerStatusesAsync(int generation, CancellationToken cancellationToken)
    {
        if (!_ready || generation != _statusGeneration || _statusRefreshRunning)
            return;
        _statusRefreshRunning = true;
        try
        {
            // The two modded routes share a population; never add their counts together.
            var server = ClubCatalog.FindServer(Config.System.ClubServer);
            if (string.IsNullOrEmpty(server.Address))
            {
                ClubOnlineCount.Text = "—";
                ClubOnlineBadge.ToolTip = "选择服务器后显示该入口的在线数据；两条模组服线路不重复计数。";
                return;
            }
            var status = await Task.Run(
                () => _statusProbe.QueryAsync(server, cancellationToken), cancellationToken);
            if (!_ready || generation != _statusGeneration || cancellationToken.IsCancellationRequested)
                return;
            if (Config.System.ClubServer == server.Id) UpdateClubStatus(status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[Conduit] 社团服务器状态刷新失败", ModBase.LogLevel.Debug);
        }
        finally
        {
            if (generation == _statusGeneration)
                _statusRefreshRunning = false;
            else if (_ready && _statusCancellation is { } cancellation)
            {
                _statusRefreshRunning = false;
                _ = RefreshServerStatusesAsync(_statusGeneration, cancellation.Token);
            }
            else
                _statusRefreshRunning = false;
        }
    }

    private void UpdateClubStatus(ClubServerStatus status)
    {
        if (!status.IsKnown)
        {
            ClubOnlineCount.Text = "未知";
            ClubOnlineBadge.ToolTip = "所选服务器入口未响应，在线人数未知。";
            return;
        }

        ClubOnlineCount.Text = status.OnlinePlayers!.Value.ToString();
        var sampleText = status.SampleNames.Count == 0
            ? "服务器未提供玩家名单。"
            : "服务器提供的玩家名单（可能不完整）：\n" + string.Join("、", status.SampleNames);
        ClubOnlineBadge.ToolTip = $"按所选服务器入口返回的人数：{status.OnlinePlayers}/{status.MaxPlayers}\n{sampleText}";
    }

    private void ShowHistory_Click(object sender, MouseButtonEventArgs e)
        => ClubUpdates.ShowHistory();
}
