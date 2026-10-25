using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.Profile.Authentication;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.Conduit;

namespace PCL.Core.Minecraft.Profile;

public partial class ProfileService
{
    internal static void EnsureSelectedMicrosoftIdentity(McProfile existing, AuthenticationResult result)
    {
        if (existing.ProfileType == ProfileType.Microsoft && !string.IsNullOrWhiteSpace(existing.Uuid) &&
            (result.ProfileType != ProfileType.Microsoft ||
             LaunchAccountSnapshot.NormalizeUuid(existing.Uuid) != LaunchAccountSnapshot.NormalizeUuid(result.Uuid)))
            throw new IdentityModelAuthenticationException("account_mismatch",
                "The authorized Microsoft account differs from the selected account. Add it separately instead of replacing the selected profile.");
    }

    internal static bool MatchesExistingAuthentication(ProfileType type, AuthenticationRequest request, McProfile? existing)
    {
        if (existing is null || existing.ProfileType != type) return false;
        if (!string.IsNullOrEmpty(request.ExpectedProfileId) && request.ExpectedProfileId != existing.ProfileId) return false;
        if (!request.ForceReselectProfile && !string.IsNullOrWhiteSpace(request.PreferredProfileId) &&
            LaunchAccountSnapshot.NormalizeUuid(request.PreferredProfileId) != LaunchAccountSnapshot.NormalizeUuid(existing.Uuid)) return false;
        if (type is ProfileType.Authlib or ProfileType.YggdrasilConnect && !string.IsNullOrWhiteSpace(request.Server) &&
            !LaunchAccountPolicy.SameAuthServer(existing.Server, request.Server)) return false;
        if (type == ProfileType.Authlib && !string.IsNullOrWhiteSpace(request.Username) &&
            !string.Equals(request.Username, existing.LoginName, StringComparison.Ordinal)) return false;
        if (type == ProfileType.YggdrasilConnect && !string.IsNullOrWhiteSpace(request.DiscoveryAddress) &&
            !string.Equals(request.DiscoveryAddress.TrimEnd('/'), existing.DiscoveryAddress?.TrimEnd('/'), StringComparison.Ordinal)) return false;
        return true;
    }

    internal static bool CanReuseCachedSession(ProfileType type, AuthenticationRequest request, McProfile? existing, DateTimeOffset now)
        => MatchesExistingAuthentication(type, request, existing) && !request.ForceRefresh && !request.ForceReselectProfile &&
            type is ProfileType.Microsoft or ProfileType.YggdrasilConnect &&
            !existing!.AuthenticationInvalidated && !string.IsNullOrWhiteSpace(existing.ProfileId) && !string.IsNullOrWhiteSpace(existing.Uuid) &&
            !string.IsNullOrWhiteSpace(existing.AccessToken) && existing.ExpiresAt > now.AddMinutes(1);

    // Shared by the actual refresh path and offline regressions; no profile-store writes here.
    internal static async Task<(AuthenticationResult? Result, bool ReuseExisting)> RefreshExistingAsync(
        IAuthenticateProvider provider, ProfileType profileType, AuthenticationRequest request,
        McProfile existing, CancellationToken token, Action<Exception>? reportFailure = null)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            return (await provider.RefreshAsync(existing, token).ConfigureAwait(false), false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IdentityModelAuthenticationException ex)
        {
            if (profileType == ProfileType.Microsoft) existing.AuthenticationInvalidated = true;
            // An invalid grant may start a fresh interactive login, never cached success.
            if (ex.Error?.StartsWith("xsts_", StringComparison.OrdinalIgnoreCase) == true ||
                (profileType == ProfileType.Microsoft &&
                 !string.Equals(ex.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase)))
                throw;

            reportFailure?.Invoke(ex);
            if (!string.Equals(ex.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase) &&
                await ConfirmCacheAsync(ex).ConfigureAwait(false))
                return (null, true);
            return (null, false);
        }
        catch (Exception ex)
        {
            if (profileType == ProfileType.Microsoft && !IsTransientNetworkFailure(ex))
                existing.AuthenticationInvalidated = true;
            // Explicit HTTP denials, malformed responses and programming errors cannot
            // become successful Microsoft authentication through the Continue button.
            if (profileType == ProfileType.Microsoft &&
                (!IsTransientNetworkFailure(ex) || existing.AuthenticationInvalidated || existing.IsExpired ||
                 string.IsNullOrWhiteSpace(existing.AccessToken))) throw;
            if (await ConfirmCacheAsync(ex).ConfigureAwait(false)) return (null, true);
            if (profileType == ProfileType.Authlib &&
                !string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password))
            {
                reportFailure?.Invoke(ex);
                return (null, false);
            }
            throw;
        }

        async Task<bool> ConfirmCacheAsync(Exception error)
        {
            token.ThrowIfCancellationRequested();
            if (request.RefreshFailureHandler is null) return false;
            var confirmed = await request.RefreshFailureHandler(error, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return confirmed;
        }
    }

    private static bool IsTransientNetworkFailure(Exception error) => error switch
    {
        TimeoutException => true,
        HttpRequestException { StatusCode: null } => true,
        HttpRequestException { StatusCode: HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests } => true,
        HttpRequestException { StatusCode: { } status } => (int)status >= 500 && (int)status <= 599,
        _ => false
    };
}
