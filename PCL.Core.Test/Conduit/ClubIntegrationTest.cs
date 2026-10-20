using System;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Conduit;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.UI.Theme;

namespace PCL.Core.Test.Conduit;

[TestClass]
public class ClubIntegrationTest
{
    [TestMethod]
    [DataRow("https://littleskin.cn/api/yggdrasil", true)]
    [DataRow("https://littleskin.cn/api/yggdrasil/authserver/", true)]
    [DataRow("https://littleskin.cn.attacker.test/api/yggdrasil", false)]
    [DataRow("https://attacker.test/littleskin.cn/api/yggdrasil", false)]
    [DataRow("http://littleskin.cn/api/yggdrasil", false)]
    [DataRow("https://littleskin.cn:8443/api/yggdrasil", false)]
    public void ProviderClassificationRequiresExactTrustedEndpoint(string address, bool expected)
        => Assert.AreEqual(expected, ClubCatalog.IsProvider(address, ClubCatalog.LittleSkinAuth));

    [TestMethod]
    public void MuaLoginUsesMemberSiteAndKeepsLegacyUnionProfilesRecognized()
    {
        Assert.AreEqual("https://skin.mualliance.ltd/api/yggdrasil", ClubCatalog.MuaAuth);
        Assert.IsTrue(ClubCatalog.IsMuaProvider(ClubCatalog.MuaAuth));
        Assert.IsTrue(ClubCatalog.IsMuaProvider(ClubCatalog.MuaUnionAuth));
        Assert.IsFalse(ClubCatalog.IsProvider(ClubCatalog.MuaUnionAuth, ClubCatalog.MuaAuth));

        var legacy = new McProfile { ProfileType = ProfileType.Authlib, Server = ClubCatalog.MuaUnionAuth };
        Assert.IsTrue(ClubCatalog.CanJoin(legacy));
        Assert.AreEqual("MUA Union", ClubCatalog.AccountSource(legacy));
    }

    [TestMethod]
    public void SamePlayerNameCanSwitchBetweenIndependentProvidersAndPersist()
    {
        var manager = new ProfileManagement<McProfile>();
        manager.LoadFromString("{\"lastUsed\":-1,\"profiles\":[]}");
        var microsoft = new McProfile { ProfileId = "ms", UserName = "SameName", ProfileType = ProfileType.Microsoft, AccessToken = "ms-test-token" };
        var mua = new McProfile { ProfileId = "mua", UserName = "SameName", ProfileType = ProfileType.Authlib, Server = ClubCatalog.MuaAuth + "/authserver", AccessToken = "mua-test-token" };
        var little = new McProfile { ProfileId = "little", UserName = "SameName", ProfileType = ProfileType.Authlib, Server = ClubCatalog.LittleSkinAuth, AccessToken = "little-test-token" };
        manager.Add(microsoft, true);
        manager.Add(mua);
        manager.Add(little);
        manager.Select(mua);
        Assert.AreEqual("MUA Union", ClubCatalog.AccountSource(manager.Current!));
        Assert.AreEqual("mua-test-token", manager.Current!.AccessToken);
        manager.Select(little);
        var reloaded = new ProfileManagement<McProfile>();
        reloaded.LoadFromString(manager.Serialize());
        reloaded.SelectAt(reloaded.LastUsed);
        Assert.AreEqual("LittleSkin", ClubCatalog.AccountSource(reloaded.Current!));
        Assert.AreEqual("little-test-token", reloaded.Current!.AccessToken);
        Assert.AreEqual(3, reloaded.GetAll().Count);
    }

    [TestMethod]
    public void UnknownServerFallsBackToNoConnection()
    {
        Assert.AreEqual("", ClubCatalog.FindServer("untrusted.example --demo").Address);
        Assert.AreEqual("", ClubCatalog.FindServer(null).Address);
        Assert.AreEqual("mc.wzbcmc.cn", ClubCatalog.FindServer("modded").Address);
        Assert.AreEqual("mc-2.wzbcmc.cn", ClubCatalog.FindServer("modded-alt").Address);
        Assert.AreEqual("build.wzbcmc.cn", ClubCatalog.FindServer("build").Address);
    }

    [TestMethod]
    public void NoConnectionOverridesOldInstanceAddressButNullPreservesLegacyLaunch()
    {
        Assert.AreEqual("", ClubCatalog.ResolveLaunchServer(ClubCatalog.FindServer("none").Address, "old.example"));
        Assert.AreEqual("mc.wzbcmc.cn", ClubCatalog.ResolveLaunchServer(ClubCatalog.FindServer("modded").Address, "old.example"));
        Assert.AreEqual("old.example", ClubCatalog.ResolveLaunchServer(null, "old.example"));
    }

    [TestMethod]
    public void ClubRequiresSupportedAuthenticationWithoutMixingProviders()
    {
        Assert.IsTrue(ClubCatalog.CanJoin(new McProfile { ProfileType = ProfileType.Microsoft }));
        Assert.IsTrue(ClubCatalog.CanJoin(new McProfile { ProfileType = ProfileType.Authlib, Server = ClubCatalog.MuaAuth }));
        Assert.IsTrue(ClubCatalog.CanJoin(new McProfile { ProfileType = ProfileType.Authlib, Server = ClubCatalog.SkinAuth }));
        Assert.IsFalse(ClubCatalog.CanJoin(new McProfile { ProfileType = ProfileType.Authlib, Server = "https://skin.wzbcmc.cn.attacker.test/api/yggdrasil" }));
        Assert.IsFalse(ClubCatalog.CanJoin(new McProfile { ProfileType = ProfileType.Offline, Server = ClubCatalog.MuaAuth }));
        Assert.IsFalse(ClubCatalog.CanJoin(new McProfile { ProfileType = ProfileType.Authlib, Server = "https://other.example/yggdrasil" }));
    }

    [TestMethod]
    public void ReleaseParsingIgnoresDraftAndNonClubEntries()
    {
        var json = JsonSerializer.Serialize(new object[]
        {
            new { draft = true, published_at = (string?)null, tag_name = "conduit-v9.0.0", html_url = "", name = "draft", body = "" },
            new { draft = false, published_at = "2026-10-03T00:00:00Z", tag_name = "v9.0.0", html_url = "https://github.com/PCL-Community/PCL-CE/releases/tag/v9.0.0", name = "upstream", body = "" },
            new { draft = false, published_at = "2026-10-02T00:00:00Z", tag_name = "conduit-v2.15.1-club.2", html_url = ClubCatalog.Repository + "/releases/tag/conduit-v2.15.1-club.2", name = "club", body = "notes" }
        });
        var release = ClubRelease.ParseLatest(json, ClubCatalog.Repository);
        Assert.IsNotNull(release);
        Assert.AreEqual("club", release.Title);
        Assert.IsTrue(release.IsNewerThan("conduit-v2.15.1-club.1"));
        Assert.IsFalse(release.IsNewerThan(ClubRelease.CurrentTag));
        Assert.IsFalse(release.IsNewerThan("conduit-v2.15.1-club.10"));
    }

    [TestMethod]
    public void BaselineMustNotOfferDowngradeFromClubBuild()
    {
        var release = new ClubRelease("conduit-v2.15.1-beta.1-baseline.20261002", "", "", "");
        Assert.IsFalse(release.IsNewerThan(ClubRelease.CurrentTag));
        Assert.IsNull(ClubRelease.ParseLatest("[]"));
    }

    [TestMethod]
    public void BackfilledOldReleaseDoesNotHideNewerVersion()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { draft = false, published_at = "2026-10-05T00:00:00Z", tag_name = "conduit-v2.15.1-club.1", html_url = ClubCatalog.Repository + "/releases/tag/conduit-v2.15.1-club.1", name = "backfill", body = "" },
            new { draft = false, published_at = "2026-10-02T00:00:00Z", tag_name = "conduit-v2.15.1-club.10", html_url = ClubCatalog.Repository + "/releases/tag/conduit-v2.15.1-club.10", name = "latest", body = "" },
            new { draft = false, published_at = "2026-10-04T00:00:00Z", tag_name = "conduit-v2.15.1-club.2", html_url = ClubCatalog.Repository + "/releases/tag/conduit-v2.15.1-club.2", name = "older", body = "" }
        });
        Assert.AreEqual("conduit-v2.15.1-club.10", ClubRelease.ParseLatest(json, ClubCatalog.Repository)!.Tag);
    }

    [TestMethod]
    public void AccountStatusDoesNotClaimUnverifiedCredentialsAreOnline()
    {
        var profile = new McProfile { ProfileType = ProfileType.Microsoft };
        Assert.AreEqual("需要重新登录", ClubCatalog.AccountStatus(profile));
        profile = new McProfile
        {
            ProfileType = ProfileType.Authlib,
            LoginName = "user@example.test",
            Password = "password-test"
        };
        Assert.AreEqual("已导入 · 首次启动时验证", ClubCatalog.AccountStatus(profile));
        profile.AccessToken = "test-not-a-real-token";
        Assert.AreEqual("已保存 · 启动时验证", ClubCatalog.AccountStatus(profile));
        profile.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        Assert.AreEqual("凭据已过期 · 启动时刷新", ClubCatalog.AccountStatus(profile));
        profile.ProfileType = ProfileType.Offline;
        Assert.AreEqual("本地档案", ClubCatalog.AccountStatus(profile));
    }

    [TestMethod]
    public void WzbcTextAndSurfaceHaveReadableContrastInBothModes()
    {
        foreach (var dark in new[] { false, true })
        {
            var colors = WzbcPalette.Colors(dark);
            foreach (var (text, background) in new[] { ("Gray1", "Background"), ("1", "White"), ("2", "Background") })
            {
                var a = Luminance(colors[text]);
                var b = Luminance(colors[background]);
                var contrast = (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
                Assert.IsGreaterThanOrEqualTo(4.5, contrast, $"{dark}: {text}/{background}");
            }
        }
    }

    private static double Luminance(string color)
    {
        var values = Enumerable.Range(0, 3).Select(i => Convert.ToInt32(color.Substring(1 + i * 2, 2), 16) / 255.0)
            .Select(c => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4)).ToArray();
        return values[0] * 0.2126 + values[1] * 0.7152 + values[2] * 0.0722;
    }
}
