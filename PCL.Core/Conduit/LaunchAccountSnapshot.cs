using System;
using System.Security.Cryptography;
using System.Text;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Conduit;

/// <summary>A single launch's identity/credential snapshot, not an entitlement grant.</summary>
public sealed class LaunchAccountSnapshot
{
    private readonly string _profileId;
    private readonly ProfileType _type;
    private readonly string _uuid;
    private readonly string _offlineName;
    private readonly string? _server;
    private readonly string _credentialHash;
    private readonly DateTimeOffset? _expiresAt;

    public LaunchAccountSnapshot(McProfile profile)
    {
        _profileId = profile.ProfileId;
        _type = profile.ProfileType;
        _uuid = NormalizeUuid(profile.Uuid);
        _offlineName = profile.ProfileType == ProfileType.Offline ? profile.UserName : string.Empty;
        _server = profile.ProfileType is ProfileType.Authlib or ProfileType.YggdrasilConnect
            ? LaunchAccountPolicy.NormalizeAuthServer(profile.Server) : null;
        _credentialHash = Hash(profile.AccessToken);
        _expiresAt = profile.ExpiresAt;
    }

    public bool MatchesIdentity(McProfile? profile) => profile is not null &&
        !string.IsNullOrWhiteSpace(_profileId) && _profileId == profile.ProfileId && _type == profile.ProfileType &&
        !string.IsNullOrWhiteSpace(_uuid) && _uuid == NormalizeUuid(profile.Uuid) &&
        (_type != ProfileType.Offline || _offlineName == profile.UserName) &&
        (_type is not (ProfileType.Authlib or ProfileType.YggdrasilConnect) ||
         (_server is not null && _server == LaunchAccountPolicy.NormalizeAuthServer(profile.Server)));

    public bool MatchesAuthenticated(McProfile? profile, DateTimeOffset now) => MatchesIdentity(profile) &&
        (_type == ProfileType.Offline || (!profile!.AuthenticationInvalidated && !string.IsNullOrWhiteSpace(profile.AccessToken) &&
            _credentialHash == Hash(profile.AccessToken) && _expiresAt == profile.ExpiresAt &&
            (_expiresAt is null || _expiresAt > now)));

    public bool MatchesResult(string? uuid, string? type, string? accessToken) =>
        !string.IsNullOrWhiteSpace(_uuid) && _uuid == NormalizeUuid(uuid) && (_type switch
        {
            ProfileType.Microsoft => type == "Microsoft" && _credentialHash == Hash(accessToken),
            ProfileType.Authlib or ProfileType.YggdrasilConnect => type == "Auth" && _credentialHash == Hash(accessToken),
            ProfileType.Offline => type == "Legacy",
            _ => false
        });

    internal static string NormalizeUuid(string? value) => Guid.TryParse(value, out var id)
        ? id.ToString("N") : value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string Hash(string? token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token ?? string.Empty)));
}
