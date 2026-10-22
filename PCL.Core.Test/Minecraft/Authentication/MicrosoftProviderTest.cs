using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.Profile.Authentication;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Test.Minecraft.Authentication;

[TestClass]
public sealed class MicrosoftProviderTest
{
    private const string ClientId = "00000000-0000-4000-8000-000000000001";

    [TestMethod]
    public async Task DeviceCodeRequestUsesOwnPublicIdAndExpectedScopes()
    {
        using var handler = new AuthHandler();
        using var client = new HttpClient(handler);
        var provider = new MicrosoftProvider(ClientId, () => client);
        var result = await provider.GetDeviceCodeAsync(CancellationToken.None);
        Assert.AreEqual("device-test", result.DeviceCode);
        StringAssert.Contains(handler.LastBody, "client_id=" + ClientId);
        StringAssert.Contains(Uri.UnescapeDataString(handler.LastBody.Replace('+', ' ')), "scope=XboxLive.signin offline_access");
        Assert.IsFalse(handler.LastBody.Contains("client_secret", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("authorization_pending")]
    [DataRow("authorization_declined")]
    [DataRow("expired_token")]
    public async Task PollErrorsRemainErrorsAndNeverStartXboxAuthentication(string error)
    {
        using var handler = new AuthHandler { TokenError = error };
        using var client = new HttpClient(handler);
        var provider = new MicrosoftProvider(ClientId, () => client);
        var device = await provider.GetDeviceCodeAsync(CancellationToken.None);
        var result = await provider.PollDeviceCodeAsync(device, CancellationToken.None);
        Assert.AreEqual(error, result!.Error);
        Assert.IsFalse(handler.Requests.Contains("/user/authenticate"));
    }

    [TestMethod]
    public async Task InvalidDeviceResponseCannotStartAuthorization()
    {
        using var handler = new AuthHandler { InvalidDevice = true };
        using var client = new HttpClient(handler);
        var provider = new MicrosoftProvider(ClientId, () => client);
        var error = await ThrowsAsync<IdentityModelAuthenticationException>(
            () => provider.GetDeviceCodeAsync(CancellationToken.None));
        Assert.AreEqual("invalid_request", error.Error);
    }

    [TestMethod]
    [DataRow(false, "old-refresh")]
    [DataRow(true, "rotated-refresh")]
    public async Task RefreshPreservesOrRotatesRefreshTokenAndRechecksOwnership(bool rotate, string expected)
    {
        using var handler = new AuthHandler { RotateRefresh = rotate };
        using var client = new HttpClient(handler);
        var provider = new MicrosoftProvider(ClientId, () => client);
        var result = await provider.RefreshAsync(new McProfile { RefreshToken = "old-refresh" }, CancellationToken.None);
        Assert.AreEqual(expected, result.RefreshToken);
        Assert.AreEqual(ProfileType.Microsoft, result.ProfileType);
        Assert.AreEqual("player-id", result.Uuid);
        CollectionAssert.Contains(handler.Requests, "/entitlements/mcstore");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MissingOrUnrelatedEntitlementFailsBeforeProfileIsFetched(bool unrelated)
    {
        using var handler = new AuthHandler { OwnsGame = false, UnrelatedEntitlement = unrelated };
        using var client = new HttpClient(handler);
        var provider = new MicrosoftProvider(ClientId, () => client);
        var error = await ThrowsAsync<IdentityModelAuthenticationException>(
            () => provider.RefreshAsync(new McProfile { RefreshToken = "old-refresh" }, CancellationToken.None));
        Assert.AreEqual("minecraft_not_owned", error.Error);
        Assert.IsFalse(handler.Requests.Contains("/minecraft/profile"));
    }

    [TestMethod]
    public async Task InvalidRefreshGrantDoesNotFallBackToOfflineIdentity()
    {
        using var handler = new AuthHandler { TokenError = "invalid_grant" };
        using var client = new HttpClient(handler);
        var provider = new MicrosoftProvider(ClientId, () => client);
        var error = await ThrowsAsync<IdentityModelAuthenticationException>(
            () => provider.RefreshAsync(new McProfile { RefreshToken = "old-refresh" }, CancellationToken.None));
        Assert.AreEqual("invalid_grant", error.Error);
        Assert.AreEqual(1, handler.Requests.Count);
    }

    [TestMethod]
    public async Task XboxRestrictionIsSurfacedWithoutCallingMinecraft()
    {
        using var handler = new AuthHandler { DenyXsts = true };
        using var client = new HttpClient(handler);
        var provider = new MicrosoftProvider(ClientId, () => client);
        var error = await ThrowsAsync<IdentityModelAuthenticationException>(
            () => provider.RefreshAsync(new McProfile { RefreshToken = "old-refresh" }, CancellationToken.None));
        Assert.AreEqual("xsts_2148916233", error.Error);
        Assert.IsFalse(handler.Requests.Contains("/authentication/login_with_xbox"));
    }

    [TestMethod]
    public async Task MissingJavaProfileIsNotTreatedAsSuccessfulLogin()
    {
        using var handler = new AuthHandler { MissingProfile = true };
        using var client = new HttpClient(handler);
        var provider = new MicrosoftProvider(ClientId, () => client);
        var error = await ThrowsAsync<IdentityModelAuthenticationException>(
            () => provider.RefreshAsync(new McProfile { RefreshToken = "old-refresh" }, CancellationToken.None));
        Assert.AreEqual("profile_not_created", error.Error);
    }

    [TestMethod]
    public async Task ExpiredProfileRequiresRefreshWithoutReusingItsAccessToken()
    {
        using var handler = new AuthHandler();
        using var client = new HttpClient(handler);
        var provider = new MicrosoftProvider(ClientId, () => client);
        Assert.IsFalse(await provider.ValidateAsync(new McProfile
        {
            AccessToken = "expired-access", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        }, CancellationToken.None));
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public async Task CancellationStopsTheFlow()
    {
        using var handler = new AuthHandler();
        using var client = new HttpClient(handler);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var provider = new MicrosoftProvider(ClientId, () => client);
        await ThrowsAsync<OperationCanceledException>(() => provider.GetDeviceCodeAsync(cancelled.Token));
    }

    private static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T error) { return error; }
        Assert.Fail("Expected " + typeof(T).Name);
        return null!;
    }

    private sealed class AuthHandler : HttpMessageHandler
    {
        public bool OwnsGame { get; init; } = true;
        public bool UnrelatedEntitlement { get; init; }
        public bool RotateRefresh { get; init; }
        public bool InvalidDevice { get; init; }
        public bool DenyXsts { get; init; }
        public bool MissingProfile { get; init; }
        public string? TokenError { get; init; }
        public List<string> Requests { get; } = [];
        public string LastBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual("https", request.RequestUri!.Scheme);
            var path = request.RequestUri.AbsolutePath;
            Requests.Add(path);
            LastBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token);
            var status = HttpStatusCode.OK;
            object response;
            if (path.EndsWith("/devicecode", StringComparison.Ordinal))
                response = new { device_code = "device-test", user_code = InvalidDevice ? null : "ABCD-EFGH",
                    verification_uri = "https://microsoft.com/devicelogin", expires_in = 600, interval = 5 };
            else if (path.EndsWith("/token", StringComparison.Ordinal))
            {
                if (TokenError is not null)
                {
                    status = HttpStatusCode.BadRequest;
                    response = new { error = TokenError, error_description = "Simulated authorization response" };
                }
                else response = new { access_token = "ms-access", refresh_token = RotateRefresh ? "rotated-refresh" : null,
                    token_type = "Bearer", expires_in = 3600 };
            }
            else if (path == "/user/authenticate" || path == "/xsts/authorize")
            {
                if (DenyXsts && path == "/xsts/authorize")
                {
                    status = HttpStatusCode.Unauthorized;
                    response = new { XErr = 2148916233L, Message = "Xbox account restriction" };
                }
                else response = new { IssueInstant = "2026-01-01T00:00:00Z", NotAfter = "2026-01-02T00:00:00Z",
                    Token = "xbox-access", DisplayClaims = new { xui = new[] { new { uhs = "user-hash" } } } };
            }
            else if (path == "/authentication/login_with_xbox")
                response = new { username = "player", roles = Array.Empty<string>(), access_token = "minecraft-access",
                    token_type = "Bearer", expires_in = 3600 };
            else if (path == "/entitlements/mcstore")
                response = new { items = OwnsGame ? new[] { new { name = "game_minecraft" } }
                    : UnrelatedEntitlement ? new[] { new { name = "unrelated_product" } } : [] };
            else if (path == "/minecraft/profile")
            {
                status = MissingProfile ? HttpStatusCode.NotFound : HttpStatusCode.OK;
                response = new { id = "player-id", name = "Player" };
            }
            else throw new AssertFailedException("Unexpected endpoint: " + request.RequestUri);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json"),
                RequestMessage = request
            };
        }
    }
}
