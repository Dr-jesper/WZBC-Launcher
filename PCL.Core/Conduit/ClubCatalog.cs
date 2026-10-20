using System;
using System.Collections.Generic;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.App;

namespace PCL.Core.Conduit;

public sealed record ClubServer(string Id, string Name, string Address)
{
    public string DisplayName => string.IsNullOrEmpty(Address) ? Name : $"{Name} · {Address}";
}

/// <summary>社团公开文档中的入口；不包含账户凭据。</summary>
public static class ClubCatalog
{
    public const string BrandName = "WZBC MC";
    public const string BrandNameEnglish = "WZBC Minecraft Club";
    public const string LauncherName = "WZBC-Launcher";
    public const string LauncherNameEnglish = "WZBC-Launcher";
    public const string GameTypeInfo = BrandName;
    public const string LegacyGameTypeInfo = "PCLCE";

    public const string Website = "https://wzbcmc.cn/";
    public const string Repository = "https://github.com/Conduit-Club/SHOU-PCL";
    public const string LegacyRepository = "https://github.com/Conduit-Club/PCL-CE-SHOU";
    // MUA Union is a server-side aggregation API. Players authenticate against
    // the member skin site, while the Union endpoint is retained for existing
    // profiles and server compatibility.
    public const string MuaAuth = "https://skin.mualliance.ltd/api/yggdrasil";
    public const string MuaUnionAuth = "https://skin.mualliance.ltd/api/union/yggdrasil";
    public const string LittleSkinAuth = "https://littleskin.cn/api/yggdrasil";
    // A downstream build must supply its own public application identifier.
    public static bool MicrosoftApiReady => !string.IsNullOrWhiteSpace(Config.System.ClubMicrosoftClientId)
        || !string.IsNullOrWhiteSpace(Secrets.MSOAuthClientId);
    // WZBC releases are independent from the source club's release stream.
    public static string ReleaseRepository => "https://github.com/Dr-jesper/WZBC-Launcher";
    public static bool UpdateNoticesReady => false;
    public const string SkinWebsite = "https://skin.wzbcmc.cn/";
    public const string SkinAuth = SkinWebsite + "api/yggdrasil";
    public static IReadOnlyList<ClubServer> Servers { get; } = Array.AsReadOnly(new[]
    {
        new ClubServer("none", "只启动游戏", ""),
        new ClubServer("modded", "模组服 · 线路一", "mc.wzbcmc.cn"),
        new ClubServer("modded-alt", "模组服 · 线路二", "mc-2.wzbcmc.cn"),
        new ClubServer("build", "校园复建服", "build.wzbcmc.cn")
    });

    public static ClubServer FindServer(string? id)
    {
        foreach (var server in Servers)
            if (server.Id == id) return server;
        return Servers[0];
    }

    public static string ResolveLaunchServer(string? selectedAddress, string? instanceAddress)
        => selectedAddress ?? instanceAddress ?? string.Empty;

    /// <summary>
    /// Migrate the old default game version type without changing an empty value
    /// or a value chosen by the user.
    /// </summary>
    public static string MigrateLegacyGameTypeInfo(string value)
        => string.Equals(value, LegacyGameTypeInfo, StringComparison.Ordinal)
            ? GameTypeInfo
            : value;

    public static bool CanJoin(McProfile profile) => profile.ProfileType == ProfileType.Microsoft
        || (profile.ProfileType is ProfileType.Authlib or ProfileType.YggdrasilConnect
            && (IsMuaProvider(profile.Server) || IsProvider(profile.Server, LittleSkinAuth) || IsProvider(profile.Server, SkinAuth)));

    /// <summary>
    /// Recognizes both the current member-site login API and the legacy Union
    /// API used by profiles created before the login endpoint was corrected.
    /// </summary>
    public static bool IsMuaProvider(string? address)
        => IsProvider(address, MuaAuth) || IsProvider(address, MuaUnionAuth);

    public static bool IsProvider(string? address, string expected)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var actual)) return false;
        var target = new Uri(expected);
        var path = actual.AbsolutePath.TrimEnd('/');
        return actual.Scheme == Uri.UriSchemeHttps && actual.Host == target.Host && actual.Port == target.Port
            && (path == target.AbsolutePath || path == target.AbsolutePath + "/authserver");
    }

    public static string AccountSource(McProfile profile) => profile.ProfileType switch
    {
        ProfileType.Microsoft => "微软正版",
        ProfileType.Offline => "离线账户 · 无法进入社团服务器",
        _ when IsProvider(profile.Server, SkinAuth) => "WZBC Skin",
        _ when IsMuaProvider(profile.Server) => "MUA Union",
        _ when IsProvider(profile.Server, LittleSkinAuth) => "LittleSkin",
        _ => "第三方 · " + (profile.ServerName ?? profile.Server ?? "自定义认证")
    };

    public static string AccountStatus(McProfile profile)
    {
        if (profile.ProfileType == ProfileType.Offline) return "本地档案";
        if (profile.ProfileType == ProfileType.Authlib &&
            string.IsNullOrWhiteSpace(profile.AccessToken) &&
            !string.IsNullOrWhiteSpace(profile.LoginName) &&
            !string.IsNullOrWhiteSpace(profile.Password))
            return "已导入 · 首次启动时验证";
        if (profile.IsExpired) return "凭据已过期 · 启动时刷新";
        if (string.IsNullOrWhiteSpace(profile.AccessToken)) return "需要重新登录";
        return "已保存 · 启动时验证";
    }
}
