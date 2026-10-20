using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.Conduit;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL;

public partial class ClubAccountPanel : UserControl
{
    private sealed record AccountListItem(McProfile Profile, string Title, string Info, string Tooltip);

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private string? _snapshot;
    private string? _lastCurrent;
    private volatile bool _otherBusy;
    private string _editingProvider = "";
    private string _filter = "microsoft";
    private bool _defaultLoginPageRequested;
    private bool _restoreAllAfterCreate;

    public ClubAccountPanel()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            RefreshAccounts();
            _timer.Start();
            RequestDefaultMicrosoftLoginPage();
        };
        Unloaded += (_, _) => { _timer.Stop(); };
        _timer.Tick += (_, _) => RefreshAccounts();
    }
    private void RefreshAccounts()
    {
        LoginStatus.Text = _otherBusy ? "正在选择或探测认证服务，请稍候。" : AuthenticationBusy ? "正在验证账户；请先在登录窗口完成或取消授权。"
            : ProfileService.IsCreatingProfile && _editingProvider != "" ? $"正在添加：{_editingProvider}。可直接点击其他登录方式切换。"
            : "";
        ProfileService.Load();
        var profiles = ProfileService.Profiles.ToArray();
        var current = ProfileService.Current?.ProfileId;
        if (current?.ToString() != _lastCurrent && _filter != "all" && !ProfileService.IsCreatingProfile && ProfileService.Current is { } active)
            _filter = Provider(active);
        if (_filter == "all" && _restoreAllAfterCreate && !_otherBusy && !ProfileService.IsCreatingProfile &&
            !AuthenticationBusy && !ModLaunch.isLaunching && ModMain.frmLaunchLeft is not null &&
            !ModMain.frmLaunchLeft.IsShowingAllProfiles)
        {
            // A successful login may select the new profile, so Current is
            // expected to differ from _lastCurrent here. Restore the complete
            // profile list regardless of that selection change.
            ModMain.frmLaunchLeft.ShowAllProfiles();
            _restoreAllAfterCreate = false;
        }
        _lastCurrent = current?.ToString();
        var snapshot = string.Join("|", profiles.Select(p => $"{p.ProfileId}:{p.UserName}:{p.Server}:{p.ServerName}:{p.SkinHeadId}:{p.ProfileType}:{ClubCatalog.AccountStatus(p)}")) + current;
        snapshot += _filter + ProfileService.IsCreatingProfile;
        var accountSelectionEnabled = !_otherBusy && !ModLaunch.isLaunching && !AuthenticationBusy;
        var allMode = _filter == "all";
        AllButton.Content = allMode ? "隐藏" : "全部";
        AccountActions.Visibility = allMode ? Visibility.Collapsed : Visibility.Visible;
        LoginStatus.Visibility = allMode || string.IsNullOrEmpty(LoginStatus.Text) ? Visibility.Collapsed : Visibility.Visible;
        AccountSelector.IsEnabled = accountSelectionEnabled;
        RemoveSelectedButton.IsEnabled = accountSelectionEnabled && GetSelectedProfile() is not null;
        if (_snapshot == snapshot) return;
        _snapshot = snapshot;
        if (_filter == "all" && ModMain.frmLaunchLeft?.IsShowingAllProfiles == true)
            ModMain.frmLoginProfile?.RefreshProfileList();
        WzbcButton.Content = $"WZBC Skin ({profiles.Count(p => Provider(p) == "wzbc")})";
        MuaButton.Content = $"MUA ({profiles.Count(p => Provider(p) == "mua")})";
        LittleButton.Content = $"LittleSkin ({profiles.Count(p => Provider(p) == "littleskin")})";
        MicrosoftButton.Content = $"微软(正版) ({profiles.Count(p => Provider(p) == "microsoft")})";
        var filtered = _filter == "all" ? profiles : profiles.Where(p => Provider(p) == _filter).ToArray();
        _suppressAccountSelection = true;
        try
        {
            var items = filtered.Select(p => new AccountListItem(
                p,
                $"{p.UserName}{(p.ProfileId == current ? " ✓" : "")}",
                $"{ClubCatalog.AccountSource(p)} · {ClubCatalog.AccountStatus(p)}",
                $"{p.UserName}（{ClubCatalog.AccountSource(p)}）\n{ClubCatalog.AccountStatus(p)}")).ToArray();
            AccountSelector.ItemsSource = items;
            AccountSelector.SelectedItem = items.FirstOrDefault(item => item.Profile.ProfileId == current);
        }
        finally
        {
            _suppressAccountSelection = false;
        }
        AccountSelector.Visibility = allMode ? Visibility.Collapsed : Visibility.Visible;
        AccountSelector.IsEnabled = accountSelectionEnabled;
        RemoveSelectedButton.IsEnabled = accountSelectionEnabled && GetSelectedProfile() is not null;
        AccountSummary.Text = $"{_filter switch { "wzbc" => "WZBC Skin", "mua" => "MUA Union", "littleskin" => "LittleSkin", "microsoft" => "微软正版", "all" => "全部账户", _ => "其他账户" }}（{filtered.Length} 个角色）";
        EmptyHint.Visibility = filtered.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string Provider(McProfile p) => p.ProfileType == ProfileType.Microsoft ? "microsoft"
        : ClubCatalog.IsProvider(p.Server, ClubCatalog.SkinAuth) ? "wzbc"
        : ClubCatalog.IsMuaProvider(p.Server) ? "mua"
        : ClubCatalog.IsProvider(p.Server, ClubCatalog.LittleSkinAuth) ? "littleskin" : "other";
    private void Provider_Click(object sender, RoutedEventArgs e)
    {
        if (!CanChangeAccount()) return;
        // Clicking a provider is an explicit navigation choice. It must not
        // inherit the pending all-profiles restoration from an earlier add.
        _restoreAllAfterCreate = false;
        var requestedFilter = ((FrameworkElement)sender).Tag?.ToString() ?? "other";
        if (requestedFilter == "all" && _filter == "all")
        {
            _filter = "microsoft";
            ModMain.frmLaunchLeft.ClearAllProfiles();
            var microsoftProfiles = ProfileService.Profiles.Where(p => Provider(p) == "microsoft").ToArray();
            if (microsoftProfiles.Length > 0)
                SelectAccount(microsoftProfiles.FirstOrDefault(p => p.ProfileId == ProfileService.Current?.ProfileId) ?? microsoftProfiles[0]);
            else
                OpenProvider("microsoft");
            RefreshAccounts();
            return;
        }

        _filter = requestedFilter;
        if (_filter == "all")
        {
            ClearEditing();
            ModMain.frmLaunchLeft.ShowAllProfiles();
            RefreshAccounts();
            return;
        }

        var profiles = ProfileService.Profiles.Where(p => Provider(p) == _filter).ToArray();
        if (profiles.Length > 0) SelectAccount(profiles.FirstOrDefault(p => p.ProfileId == ProfileService.Current?.ProfileId) ?? profiles[0]);
        else OpenProvider(_filter);
        RefreshAccounts();
    }

    private bool _suppressAccountSelection;
    private void AccountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressAccountSelection || e.AddedItems.Count == 0) return;
        if (!CanChangeAccount())
        {
            _RestoreCurrentSelection();
            return;
        }
        _restoreAllAfterCreate = false;
        if (e.AddedItems[0] is AccountListItem item) SelectAccount(item.Profile);
    }

    private void _RestoreCurrentSelection()
    {
        var currentId = ProfileService.Current?.ProfileId;
        _suppressAccountSelection = true;
        try
        {
            AccountSelector.SelectedItem = AccountSelector.Items.OfType<AccountListItem>()
                .FirstOrDefault(item => item.Profile.ProfileId == currentId);
        }
        finally
        {
            _suppressAccountSelection = false;
        }
    }
    private void SelectAccount(McProfile profile)
    {
        ClearEditing();
        ModMain.frmLaunchLeft.ClearAllProfiles();
        ProfileService.Select(profile);
        ModLaunch.mcLoginMsLoader.State = ModBase.LoadState.Waiting;
        ModLaunch.mcLoginAuthLoader.State = ModBase.LoadState.Waiting;
        ModLaunch.mcLoginLegacyLoader.State = ModBase.LoadState.Waiting;
        ModMain.frmLaunchLeft.RefreshPage(false);
        RefreshAccounts();
    }
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!CanChangeAccount()) return;
        _restoreAllAfterCreate = _filter == "all";
        OpenProvider(_filter == "all" ? "other" : _filter);
    }
    private void OpenProvider(string provider)
    {
        ClearEditing();
        ModMain.frmLaunchLeft.ClearAllProfiles();
        if (provider == "other")
        {
            ModMain.frmLaunchLeft.RefreshPage(false);
            _otherBusy = true;
            ModBase.RunInNewThread(() =>
            {
                try { ProfileUi.CreateProfile(); }
                finally { ModBase.RunInUi(() => { _otherBusy = false; RefreshAccounts(); }); }
            });
            return;
        }
        _editingProvider = provider switch { "microsoft" => "微软正版", "wzbc" => "WZBC Skin", "mua" => "MUA Union", _ => "LittleSkin" };
        ProfileService.IsCreatingProfile = true;
        if (provider != "microsoft")
        {
            PageLoginAuth.draggedAuthServer = provider switch { "wzbc" => ClubCatalog.SkinAuth, "mua" => ClubCatalog.MuaAuth, _ => ClubCatalog.LittleSkinAuth };
            PageLoginAuth.draggedAuthServerOAuthSupported = null;
        }
        ModMain.frmLaunchLeft.RefreshPage(false, provider == "microsoft" ? ModLaunch.McLoginType.Ms : ModLaunch.McLoginType.Auth);
        RefreshAccounts();
    }
    private static bool AuthenticationBusy => ModMain.frmLoginMs?.IsAuthenticating == true
        || ModMain.frmLoginAuth?.IsAuthenticating == true
        || ModLaunch.mcLoginMsLoader.State == ModBase.LoadState.Loading
        || ModLaunch.mcLoginAuthLoader.State == ModBase.LoadState.Loading;

    private bool CanChangeAccount()
    {
        if (_otherBusy) { HintService.Hint("正在选择或探测认证服务，请稍候。"); return false; }
        if (ModLaunch.isLaunching) { HintService.Hint("游戏正在启动，请稍后切换账户。"); return false; }
        if (AuthenticationBusy) { HintService.Hint("正在验证账户，请先在登录窗口完成或取消授权。"); return false; }
        return true;
    }

    private void ClearEditing()
    {
        ModMain.frmLoginAuth?.ClearEditing();
        ProfileService.IsCreatingProfile = false;
        _editingProvider = "";
    }

    private void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanChangeAccount()) return;
        var profile = GetSelectedProfile();
        if (profile is null) return;
        if (ModMain.MyMsgBox($"从本机移除 {profile.UserName}（{ClubCatalog.AccountSource(profile)}）？之后可以重新登录添加。",
                "移除本机账户", "移除", "取消") != 1) return;
        ClearEditing();
        ModMain.frmLaunchLeft.ClearAllProfiles();
        ProfileService.Remove(profile);
        if (_filter == "all")
            ModMain.frmLaunchLeft.ShowAllProfiles();
        ModLaunch.mcLoginMsLoader.State = ModBase.LoadState.Waiting;
        ModLaunch.mcLoginAuthLoader.State = ModBase.LoadState.Waiting;
        ModLaunch.mcLoginLegacyLoader.State = ModBase.LoadState.Waiting;
        ModMain.frmLaunchLeft.RefreshPage(false);
        RefreshAccounts();
    }
    private void RequestDefaultMicrosoftLoginPage()
    {
        if (_defaultLoginPageRequested || ProfileService.Current is not null || ProfileService.IsCreatingProfile)
            return;
        _defaultLoginPageRequested = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            if (ProfileService.Current is null && !ProfileService.IsCreatingProfile &&
                ModMain.frmLaunchLeft is not null && !ModMain.frmLaunchLeft.IsShowingAllProfiles)
                ModMain.frmLaunchLeft.RefreshPage(false, ModLaunch.McLoginType.Ms);
        }));
    }


    private McProfile? GetSelectedProfile()
        => _filter == "all" ? ProfileService.Current : (AccountSelector.SelectedItem as AccountListItem)?.Profile;
}
