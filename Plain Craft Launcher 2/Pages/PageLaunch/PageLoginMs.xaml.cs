using System.Security.Authentication;
using System.Windows;
using System.Runtime.ExceptionServices;
using PCL.Core.App.Localization;
using PCL.Core.Conduit;
using PCL.Core.Minecraft.Profile;

namespace PCL;

public partial class PageLoginMs
{
    public bool IsAuthenticating { get; private set; }
    public PageLoginMs()
    {
        // Handles
        InitializeComponent();
        BtnBack.Click += BtnBack_Click;
        BtnLogin.Click += BtnLogin_Click;
        Loaded += (_, _) => RefreshAvailability();
    }

    private void RefreshAvailability()
    {
        var available = ClubCatalog.MicrosoftApiReady;
        BtnLogin.IsEnabled = available;
        MicrosoftUnavailable.Visibility = available ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BtnBack_Click(object sender, EventArgs e)
    {
        ProfileService.IsCreatingProfile = false;
        ModBase.RunInUi(() => ModMain.frmLaunchLeft.RefreshPage(true));
    }

    private void BtnLogin_Click(object sender, EventArgs e)
    {
        if (!ClubCatalog.MicrosoftApiReady)
        {
            HintService.Hint("Microsoft 应用已注册，但 Minecraft Services 审核未获批准；本构建未启用微软登录。", HintType.Warning);
            return;
        }
        if (IsAuthenticating) return;
        IsAuthenticating = true;
        BtnLogin.IsEnabled = false;
        BtnBack.Visibility = Visibility.Collapsed;
        BtnLogin.Text = Lang.Number(0d, "P0");
        ModBase.RunInNewThread(() =>
        {
            var previousProfile = ProfileService.Current;
            var loginSucceeded = false;
            try
            {
                ProfileService.Select(null);
                ModLaunch.mcLoginMsLoader.Start(ProfileUi.GetLoginData(ModLaunch.McLoginType.Ms), true);
                while (ModLaunch.mcLoginMsLoader.State == ModBase.LoadState.Loading)
                {
                    ModBase.RunInUi(() => BtnLogin.Text = Lang.Number(ModLaunch.mcLoginMsLoader.Progress, "P0"));
                    Thread.Sleep(50);
                }

                if (ModLaunch.mcLoginMsLoader.State == ModBase.LoadState.Finished)
                {
                    loginSucceeded = true;
                    ModBase.RunInUi(() => ModMain.frmLaunchLeft.RefreshPage(true));
                }
                else if (ModLaunch.mcLoginMsLoader.State == ModBase.LoadState.Aborted)
                    throw new ThreadInterruptedException();
                else if (ModLaunch.mcLoginMsLoader.Error is null)
                    throw new InvalidOperationException(Lang.Text("Launch.Account.Microsoft.Error.Unknown"));
                else
                    ExceptionDispatchInfo.Capture(ModLaunch.mcLoginMsLoader.Error).Throw();
            }
            catch (ThreadInterruptedException ex)
            {
                if (!loginSucceeded && previousProfile is not null) ProfileService.Select(previousProfile);
                HintService.Hint(Lang.Text("Launch.Account.LoginCancelled"));
            }
            catch (Exception ex)
            {
                if (!loginSucceeded && previousProfile is not null) ProfileService.Select(previousProfile);
                if (ex.Message == "$$")
                {
                }
                else if (ex.Message.StartsWith("$"))
                {
                    HintService.Hint(
                        Lang.Text("Launch.Account.Microsoft.LoginFailed.WithDetail", ex.Message.TrimStart('$')),
                        HintType.Error);
                }
                else if (ex is AuthenticationException && ex.Message.ContainsF("SSL/TLS"))
                {
                    ModBase.Log(
                        ex,
                        $"{Lang.Text("Launch.Account.Microsoft.LoginFailed.Message")}\r\n{ex.Message}",
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Launch.Account.Microsoft.Error.OperationFailed"));
                }
                else
                {
                    ModBase.Log(
                        ex,
                        Lang.Text("Launch.Account.Microsoft.LoginFailed.Title"),
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Launch.Account.Microsoft.LoginFailed.Title"));
                }
            }
            finally
            {
                ProfileService.IsCreatingProfile = false;
                ModBase.RunInUi(() =>
                {
                    IsAuthenticating = false;
                    BtnLogin.IsEnabled = true;
                    BtnBack.Visibility = Visibility.Visible;
                    BtnLogin.Text = Lang.Text("Launch.Account.Login");
                });
            }
        }, "Ms Login");
    }
}
