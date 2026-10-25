using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.IoC;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.Minecraft.Profile.Authentication;
using PCL.Core.Utils;
using PCL.Core.Utils.Secret;

namespace PCL.Core.Minecraft.Profile;

[LifecycleScope("profile", "档案服务")]
[LifecycleService(LifecycleState.Loaded)]
public partial class ProfileService
{
    private static readonly ProfileManagement<McProfile> _profiles = new();
    private static readonly object _saveLock = new();
    private static bool _canPersist = true;

    public static IReadOnlyList<McProfile> Profiles => _profiles.GetAll();
    public static McProfile? Current => _profiles.Current;
    public static int LastUsedProfile => _profiles.LastUsed;
    public static bool IsLoaded => _profiles.IsLoaded;
    public static bool IsCreatingProfile { get; set; }

    [LifecycleStart]
    private static Task _Start()
    {
        Load();
        return Task.CompletedTask;
    }

    [LifecycleStop]
    private static void _Stop()
    {
        if (IsLoaded) Save();
    }

    public static void Load()
    {
        if (_profiles.IsLoaded) return;

        var encryptedProvider = ConfigService.GetProvider(ConfigSource.SharedEncrypt);
        var hasConfiguredProfiles = encryptedProvider.GetValue<string>("Profile", out var configured);
        if (!hasConfiguredProfiles && ConfigService.GetProvider(ConfigSource.Shared).GetValue<string>("Profile", out var rawProfile))
        {
            if (_TryLoad(rawProfile))
            {
                _EnsureProfileIds();
                Save();
                return;
            }
            Context.Error("档案加密配置无法读取；为避免覆盖原始凭据，本次不会保存档案配置。");
            _canPersist = false;
            _profiles.LoadFromString("{\"lastUsed\":-1,\"profiles\":[]}");
            return;
        }
        if (hasConfiguredProfiles)
        {
            if (_TryLoad(configured))
            {
                _EnsureProfileIds();
                return;
            }
            Context.Error("档案配置格式无效；为避免覆盖原始凭据，本次不会保存档案配置。");
            _canPersist = false;
            _profiles.LoadFromString("{\"lastUsed\":-1,\"profiles\":[]}");
            return;
        }

        if (!_TryMigrateLegacy(out var migrated))
        {
            Context.Error("迁移旧档案失败；为避免覆盖原始凭据，本次不会保存档案配置。");
            _canPersist = false;
            _profiles.LoadFromString("{\"lastUsed\":-1,\"profiles\":[]}");
            return;
        }
        _profiles.LoadFromString(migrated ?? "{\"lastUsed\":-1,\"profiles\":[]}");
        Save();
    }

    public static void Save()
    {
        if (!IsLoaded || !_canPersist) return;
        lock (_saveLock)
            Config.System.ProfilesConfig.SetValue(_profiles.Serialize(), forceNewValue: true);
    }

    public static void Add(McProfile profile, bool select = true)
    {
        _EnsureLoaded();
        if (string.IsNullOrWhiteSpace(profile.ProfileId)) profile.ProfileId = Guid.NewGuid().ToString("N");
        _profiles.Add(profile, select);
        Save();
    }

    public static void Update(McProfile origin, McProfile current)
    {
        _EnsureLoaded();
        _profiles.Update(origin, current);
        Save();
    }

    public static void Remove(McProfile profile)
    {
        _EnsureLoaded();
        _profiles.Delete(profile);
        Save();
    }

    public static void Select(McProfile? profile)
    {
        _EnsureLoaded();
        _profiles.Select(profile);
        Save();
    }

    public static void SelectAt(int index)
    {
        _EnsureLoaded();
        _profiles.SelectAt(index);
        Save();
    }

    public static void Clear()
    {
        _EnsureLoaded();
        _profiles.Clear();
        Save();
    }

    public static async Task<McProfile> AuthenticateAsync(ProfileType profileType, AuthenticationRequest request,
        McProfile? existing, bool select, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);
        _EnsureLoaded();

        token.ThrowIfCancellationRequested();
        var canUseExisting = MatchesExistingAuthentication(profileType, request, existing);
        if (!canUseExisting) existing = null;

        if (CanReuseCachedSession(profileType, request, existing, DateTimeOffset.UtcNow))
        {
            if (select) Select(existing);
            return existing;
        }

        var provider = await _CreateProviderAsync(profileType, request, existing, token).ConfigureAwait(false);
        AuthenticationResult? result = null;

        if (canUseExisting && profileType == ProfileType.Authlib && !request.ForceRefresh &&
            !request.ForceReselectProfile)
        {
            try
            {
                if (await provider.ValidateAsync(existing!, token).ConfigureAwait(false))
                {
                    if (select) Select(existing!);
                    return existing!;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Context.Warn("验证第三方档案失败，将尝试刷新", ex);
            }
        }

        if (canUseExisting && !string.IsNullOrWhiteSpace(existing!.AccessToken) &&
            (!request.ForceReselectProfile || profileType != ProfileType.Authlib))
        {
            (AuthenticationResult? Result, bool ReuseExisting) refresh;
            try
            {
                refresh = await RefreshExistingAsync(provider, profileType, request, existing!, token,
                    ex => Context.Warn("刷新档案失败，将尝试重新认证", ex)).ConfigureAwait(false);
            }
            finally
            {
                if (existing!.AuthenticationInvalidated) Save();
            }
            if (refresh.ReuseExisting)
            {
                if (select) Select(existing!);
                return existing!;
            }
            result = refresh.Result;
        }

        if (result is null)
        {
            var loginRequest = request with
            {
                RefreshToken = null,
                IdToken = existing?.IdToken ?? request.IdToken,
                PreferredProfileId = request.PreferredProfileId ??
                    (!request.ForceReselectProfile && profileType == ProfileType.Authlib
                        ? existing?.Uuid
                        : null)
            };
            result = await provider.AuthenticateAsync(loginRequest, token).ConfigureAwait(false);
        }

        if (canUseExisting && profileType == ProfileType.Microsoft) EnsureSelectedMicrosoftIdentity(existing!, result);
        var matched = canUseExisting ? existing : _FindMatchingProfile(result);
        token.ThrowIfCancellationRequested();
        return ApplyAuthenticationResult(result, matched, select, token, request.ImportAvailableProfiles);
    }

    public static McProfile ApplyAuthenticationResult(AuthenticationResult result, McProfile? existing = null,
        bool select = true, CancellationToken cancellationToken = default, bool importAvailableProfiles = false)
    {
        ArgumentNullException.ThrowIfNull(result);
        _EnsureLoaded();
        cancellationToken.ThrowIfCancellationRequested();
        var profile = existing?.Clone() ?? new McProfile { ProfileId = Guid.NewGuid().ToString("N") };
        profile.ProfileType = result.ProfileType;
        profile.UserName = result.UserName;
        profile.Uuid = result.Uuid;
        profile.AccessToken = result.AccessToken;
        profile.RefreshToken = result.RefreshToken;
        profile.ClientToken = result.ClientToken;
        profile.TokenType = result.TokenType;
        profile.ExpiresAt = result.ExpiresAt;
        profile.RawJson = result.RawJson;
        profile.AuthenticationInvalidated = false;
        profile.Server = result.Server ?? profile.Server;
        profile.ServerName = result.ServerName ?? profile.ServerName;
        profile.LoginName = result.LoginName ?? profile.LoginName;
        profile.Password = result.Password ?? profile.Password;
        profile.Provider = result.Provider ?? profile.Provider;
        profile.DiscoveryAddress = result.DiscoveryAddress ?? profile.DiscoveryAddress;
        profile.IdToken = result.IdToken ?? profile.IdToken;

        // Prepare every candidate before the first profile write. Once the
        // commit starts no further cancellation checks can leave a partial
        // candidate import behind.
        var plan = importAvailableProfiles
            ? PlanAuthenticationCandidates(result, profile, Profiles, importAvailableProfiles, cancellationToken)
            : new AuthenticationCandidateImportPlan([], []);
        cancellationToken.ThrowIfCancellationRequested();
        if (existing is null) _profiles.Add(profile, select);
        else
        {
            _profiles.Update(existing, profile);
            if (select) _profiles.Select(profile);
        }
        foreach (var (origin, current) in plan.Updates) _profiles.Update(origin, current);
        foreach (var addition in plan.Additions) _profiles.Add(addition, select: false);
        Save();
        return profile;
    }

    public static bool HasMicrosoftProfile => Profiles.Any(p => p.ProfileType == ProfileType.Microsoft);

    private static async Task<IAuthenticateProvider> _CreateProviderAsync(ProfileType profileType,
        AuthenticationRequest request, McProfile? existing, CancellationToken token)
    {
        switch (profileType)
        {
            case ProfileType.Microsoft:
                return new MicrosoftProvider();
            case ProfileType.Authlib:
                return new AuthlibProvider(request.Server ?? existing?.Server ??
                    throw new InvalidOperationException("Authlib profile has no server."));
            case ProfileType.YggdrasilConnect:
            {
                var provider = new YggdrasilConnectProvider(
                    request.DiscoveryAddress ?? existing?.DiscoveryAddress ??
                    throw new InvalidOperationException("Yggdrasil Connect profile has no discovery address."),
                    request.ClientId,
                    request.Server ?? existing?.Server);
                await provider.InitializeAsync(token).ConfigureAwait(false);
                return provider;
            }
            default:
                throw new InvalidOperationException($"Profile type '{profileType}' does not require remote authentication.");
        }
    }

    private static McProfile? _FindMatchingProfile(AuthenticationResult result)
        => _FindMatchingProfile(result.ProfileType, result.Uuid, result.Server);

    private static McProfile? _FindMatchingProfile(ProfileType profileType, string? uuid, string? server)
    {
        var profileId = _NormalizeProfileId(uuid);
        if (string.IsNullOrEmpty(profileId)) return null;
        var serverId = _NormalizeServerIdentity(server);
        return Profiles.FirstOrDefault(profile =>
            profile.ProfileType == profileType &&
            _NormalizeProfileId(profile.Uuid) == profileId &&
            (profileType == ProfileType.Microsoft ||
             _NormalizeServerIdentity(profile.Server) == serverId));
    }

    internal sealed record AuthenticationCandidateImportPlan(
        IReadOnlyList<(McProfile Origin, McProfile Current)> Updates,
        IReadOnlyList<McProfile> Additions);

    /// <summary>
    /// Builds the complete candidate change set before it is persisted. This
    /// keeps cancellation and duplicate handling testable without touching
    /// the encrypted profile store.
    /// </summary>
    internal static AuthenticationCandidateImportPlan PlanAuthenticationCandidates(
        AuthenticationResult result, McProfile selected, IReadOnlyList<McProfile> existingProfiles,
        bool importAvailableProfiles, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(existingProfiles);
        cancellationToken.ThrowIfCancellationRequested();

        // Only password authentication can safely seed another profile. A
        // refresh or OAuth result without credentials must never create a
        // profile that appears launchable but has no way to re-authenticate.
        if (!importAvailableProfiles || result.ProfileType != ProfileType.Authlib ||
            result.AvailableProfiles.Count == 0 ||
            string.IsNullOrWhiteSpace(result.Server) || string.IsNullOrWhiteSpace(result.LoginName) ||
            string.IsNullOrWhiteSpace(result.Password))
            return new([], []);

        var selectedId = _NormalizeProfileId(selected.Uuid);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var updates = new List<(McProfile Origin, McProfile Current)>();
        var additions = new List<McProfile>();
        foreach (var candidate in result.AvailableProfiles)
        {
            var candidateId = _NormalizeProfileId(candidate.Id);
            if (string.IsNullOrEmpty(candidateId) || candidateId == selectedId || !seen.Add(candidateId)) continue;

            var existing = existingProfiles.FirstOrDefault(profile =>
                profile.ProfileType == ProfileType.Authlib &&
                _NormalizeProfileId(profile.Uuid) == candidateId &&
                _NormalizeServerIdentity(profile.Server) == _NormalizeServerIdentity(result.Server));
            if (existing is not null)
            {
                // Keep any token already earned by this profile. Its password
                // remains the independent fallback if that token expires.
                var updated = existing.Clone();
                updated.UserName = candidate.Name;
                updated.Uuid = candidate.Id;
                updated.Server = result.Server;
                updated.ServerName = result.ServerName ?? updated.ServerName;
                updated.LoginName = result.LoginName;
                updated.Password = result.Password;
                updated.Provider = result.Provider ?? updated.Provider;
                updates.Add((existing, updated));
                continue;
            }

            additions.Add(new McProfile
            {
                ProfileId = Guid.NewGuid().ToString("N"),
                ProfileType = ProfileType.Authlib,
                UserName = candidate.Name,
                Uuid = candidate.Id,
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                ClientToken = string.Empty,
                TokenType = result.TokenType,
                Server = result.Server,
                ServerName = result.ServerName,
                LoginName = result.LoginName,
                Password = result.Password,
                Provider = result.Provider
            });
        }

        // A cancellation that arrives here leaves the profile list untouched.
        cancellationToken.ThrowIfCancellationRequested();
        return new(updates, additions);
    }

    private static string _NormalizeProfileId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return string.Empty;
        if (Guid.TryParse(id.Trim(), out var guid)) return guid.ToString("N");
        return id.Trim().ToUpperInvariant();
    }

    private static string _NormalizeServerIdentity(string? server)
    {
        if (string.IsNullOrWhiteSpace(server)) return string.Empty;
        var value = server.Trim().TrimEnd('/');
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            var path = uri.AbsolutePath.TrimEnd('/');
            if (path.EndsWith("/authserver", StringComparison.OrdinalIgnoreCase))
                path = path[..^"/authserver".Length].TrimEnd('/');
            var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
            return $"{uri.Scheme.ToLowerInvariant()}://{uri.Host.ToLowerInvariant()}{port}{path}".TrimEnd('/');
        }
        return value;
    }

    private static void _EnsureLoaded()
    {
        if (!IsLoaded) Load();
    }

    private static bool _TryLoad(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            var node = JsonCompat.ParseNode(json);
            if (node is not JsonObject root || root["profiles"] is not JsonArray)
                return false;
            _profiles.LoadFromString(json);
            return true;
        }
        catch (Exception ex)
        {
            Context.Warn("读取新档案配置失败，将尝试迁移旧档案", ex);
            return false;
        }
    }

    private static void _EnsureProfileIds()
    {
        var changed = false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in Profiles)
        {
            if (!string.IsNullOrWhiteSpace(profile.ProfileId) && ids.Add(profile.ProfileId)) continue;
            do profile.ProfileId = Guid.NewGuid().ToString("N");
            while (!ids.Add(profile.ProfileId));
            changed = true;
        }
        if (changed) Save();
    }

    private static bool _TryMigrateLegacy(out string? migrated)
    {
        migrated = null;
        var candidates = new[]
        {
            Path.Combine(Paths.OldSharedData, "profiles.json"),
            Path.Combine(Paths.SharedData, "profiles.json")
        };
        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null) return true;

        try
        {
            var root = JsonCompat.ParseNode(File.ReadAllText(path)) as JsonObject;
            if (root?["profiles"] is not JsonArray oldProfiles) return false;
            var result = new ProfileJson<McProfile>
            {
                LastUsed = root["lastUsed"]?.GetValue<int>() ?? -1
            };

            foreach (var node in oldProfiles.OfType<JsonObject>())
            {
                var type = node["type"]?.ToString()?.ToLowerInvariant();
                var profile = new McProfile
                {
                    ProfileId = Guid.NewGuid().ToString("N"),
                    UserName = node["username"]?.ToString() ?? string.Empty,
                    Uuid = node["uuid"]?.ToString() ?? string.Empty,
                    Description = node["desc"]?.ToString() ?? string.Empty,
                    SkinHeadId = node["skinHeadId"]?.ToString() ?? string.Empty,
                    Server = node["server"]?.ToString(),
                    ServerName = node["serverName"]?.ToString(),
                    LoginName = _Decrypt(node["name"]?.ToString()),
                    Password = _Decrypt(node["password"]?.ToString()),
                    ClientToken = _Decrypt(node["clientToken"]?.ToString()),
                    AccessToken = _Decrypt(node["accessToken"]?.ToString()),
                    RefreshToken = _Decrypt(node["refreshToken"]?.ToString()),
                    RawJson = _Decrypt(node["rawJson"]?.ToString()),
                    TokenType = "Bearer",
                    ProfileType = type switch
                    {
                        "microsoft" => ProfileType.Microsoft,
                        "authlib" => ProfileType.Authlib,
                        _ => ProfileType.Offline
                    }
                };

                if (node["expires"]?.GetValue<long>() is { } expires && expires > 0)
                {
                    try
                    {
                        profile.ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expires);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        profile.ExpiresAt = null;
                    }
                }
                result.Profiles.Add(profile);
            }

            migrated = JsonSerializer.Serialize(result, JsonCompat.SerializerOptions);
            return true;
        }
        catch (Exception ex)
        {
            Context.Error("迁移旧档案失败", ex);
            return false;
        }
    }

    private static string _Decrypt(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        try { return EncryptHelper.SecretDecrypt(value); }
        catch { return value; }
    }
}
