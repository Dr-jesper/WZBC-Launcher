using System;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Conduit;

public enum LaunchAccountRejection
{
    None,
    NoProfile,
    RequireMicrosoft,
    RequireThirdParty,
    RequireMicrosoftOrThirdParty,
    AuthServerMismatch,
    ClubAccountRequired,
    UnknownProfileType
}

public sealed record LaunchAccountRules(int InstanceRequirement = 0, string? AuthServer = null,
    bool RequiresMicrosoft = false, bool UseClubAccount = false, string? ServerAddress = null);

/// <summary>
/// One account policy for all launcher entry points. Skin-site identity is not
/// Microsoft entitlement evidence; offline identities retain upstream local use.
/// </summary>
public static class LaunchAccountPolicy
{
    public static LaunchAccountRejection Evaluate(McProfile? profile, LaunchAccountRules rules)
    {
        if (profile is null) return LaunchAccountRejection.NoProfile;
        if (!Enum.IsDefined(profile.ProfileType)) return LaunchAccountRejection.UnknownProfileType;
        var microsoft = profile.ProfileType == ProfileType.Microsoft;
        var thirdParty = profile.ProfileType is ProfileType.Authlib or ProfileType.YggdrasilConnect;
        if (rules.RequiresMicrosoft)
            return microsoft ? LaunchAccountRejection.None : LaunchAccountRejection.RequireMicrosoft;
        // Explicit club routes retain upstream precedence over old instance auth locks.
        // Recognize configured club hosts for server-card and custom-event routes too.
        if (rules.UseClubAccount || IsClubServer(rules.ServerAddress))
            return ClubCatalog.CanJoin(profile) ? LaunchAccountRejection.None : LaunchAccountRejection.ClubAccountRequired;
        return rules.InstanceRequirement switch
        {
            1 when !microsoft => LaunchAccountRejection.RequireMicrosoft,
            2 when !thirdParty => LaunchAccountRejection.RequireThirdParty,
            3 when !microsoft && !thirdParty => LaunchAccountRejection.RequireMicrosoftOrThirdParty,
            2 or 3 when thirdParty && !SameAuthServer(profile.Server, rules.AuthServer) => LaunchAccountRejection.AuthServerMismatch,
            _ => LaunchAccountRejection.None
        };
    }

    public static bool IsClubServer(string? address)
    {
        if (string.IsNullOrWhiteSpace(address) ||
            !Uri.TryCreate("minecraft://" + address.Trim(), UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath is not ("" or "/")) return false;
        var host = uri.IdnHost.TrimEnd('.');
        foreach (var server in ClubCatalog.Servers)
            if (!string.IsNullOrEmpty(server.Address) &&
                string.Equals(host, server.Address, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public static bool SameAuthServer(string? left, string? right)
        => NormalizeAuthServer(left) is { } expected && expected == NormalizeAuthServer(right);

    internal static string? NormalizeAuthServer(string? address)
    {
        if (!Uri.TryCreate(address?.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return null;
        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.EndsWith("/authserver", StringComparison.OrdinalIgnoreCase))
            path = path[..^"/authserver".Length].TrimEnd('/');
        return $"{uri.Scheme}://{uri.IdnHost.ToLowerInvariant()}:{uri.Port}{path}";
    }
}
