using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Authentication;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Test.Minecraft.Authentication;

[TestClass]
public sealed class ProfileRefreshFailureTest
{
    [TestMethod]
    [DataRow(400)]
    [DataRow(401)]
    [DataRow(403)]
    [DataRow(404)]
    public async Task HttpDenialNeverOffersCachedSuccess(int status)
    {
        var error = new HttpRequestException("denied", null, (HttpStatusCode)status);
        var calls = 0;
        try
        {
            await Refresh(error, (_, _) => { calls++; return Task.FromResult(true); });
            Assert.Fail("Expected rejection.");
        }
        catch (HttpRequestException actual)
        {
            Assert.AreSame(error, actual);
            Assert.AreEqual(0, calls);
        }
    }

    [TestMethod]
    public async Task InvalidGrantRequestsFreshAuthenticationWithoutReusingCache()
    {
        var calls = 0;
        var outcome = await Refresh(new IdentityModelAuthenticationException("invalid_grant", "revoked"),
            (_, _) => { calls++; return Task.FromResult(true); });
        Assert.IsNull(outcome.Result);
        Assert.IsFalse(outcome.ReuseExisting);
        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public async Task InvalidResponseCannotOfferCachedSuccess()
    {
        var calls = 0;
        try
        {
            await Refresh(new InvalidDataException("malformed response"),
                (_, _) => { calls++; return Task.FromResult(true); });
            Assert.Fail("Expected invalid response rejection.");
        }
        catch (InvalidDataException) { Assert.AreEqual(0, calls); }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(408)]
    [DataRow(429)]
    [DataRow(503)]
    public async Task TemporaryNetworkFailureStillRequiresExplicitCacheChoice(int status)
    {
        var error = new HttpRequestException("temporary", null, status == 0 ? null : (HttpStatusCode)status);
        var calls = 0;
        var outcome = await Refresh(error, (_, _) => { calls++; return Task.FromResult(true); });
        Assert.IsTrue(outcome.ReuseExisting);
        Assert.IsNull(outcome.Result);
        Assert.AreEqual(1, calls);
        try
        {
            await Refresh(error, (_, _) => Task.FromResult(false));
            Assert.Fail("Declining must not reuse cache.");
        }
        catch (HttpRequestException actual) { Assert.AreSame(error, actual); }
    }

    [TestMethod]
    public async Task CancellationAfterCachePromptCannotBecomeSuccess()
    {
        using var cancelled = new CancellationTokenSource();
        try
        {
            await Refresh(new HttpRequestException("unreachable"), (_, _) =>
            {
                cancelled.Cancel();
                return Task.FromResult(true);
            }, cancelled.Token);
            Assert.Fail("Expected cancellation.");
        }
        catch (OperationCanceledException) { }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TemporaryFailureCannotRestoreAnExpiredOrEmptyMicrosoftSession(bool expired)
    {
        var calls = 0;
        var existing = new McProfile
        {
            ProfileType = ProfileType.Microsoft, AccessToken = expired ? "expired-test" : "",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expired ? -5 : 20)
        };
        try
        {
            await ProfileService.RefreshExistingAsync(new FailedProvider(new HttpRequestException("unreachable")),
                ProfileType.Microsoft, new AuthenticationRequest
                {
                    RefreshFailureHandler = (_, _) => { calls++; return Task.FromResult(true); }
                }, existing, CancellationToken.None);
            Assert.Fail("An expired or empty session cannot become cached success.");
        }
        catch (HttpRequestException) { Assert.AreEqual(0, calls); }
    }

    [TestMethod]
    public async Task RejectedSessionCannotBeReusedOnAFollowingNetworkFailure()
    {
        var existing = new McProfile
        {
            ProfileType = ProfileType.Microsoft, ProfileId = "rejected-test", Uuid = "role-test",
            AccessToken = "old-test", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        var calls = 0;
        var request = new AuthenticationRequest { RefreshFailureHandler = (_, _) => { calls++; return Task.FromResult(true); } };
        try
        {
            await ProfileService.RefreshExistingAsync(new FailedProvider(new IdentityModelAuthenticationException("minecraft_not_owned", "denied")),
                ProfileType.Microsoft, request, existing, CancellationToken.None);
            Assert.Fail("Expected explicit rejection.");
        }
        catch (IdentityModelAuthenticationException) { Assert.IsTrue(existing.AuthenticationInvalidated); }
        try
        {
            await ProfileService.RefreshExistingAsync(new FailedProvider(new HttpRequestException("offline")),
                ProfileType.Microsoft, request, existing, CancellationToken.None);
            Assert.Fail("Previous denial must not be hidden by a later network failure.");
        }
        catch (HttpRequestException) { Assert.AreEqual(0, calls); }
    }

    private static Task<(AuthenticationResult? Result, bool ReuseExisting)> Refresh(Exception error,
        Func<Exception, CancellationToken, Task<bool>> callback, CancellationToken token = default) =>
        ProfileService.RefreshExistingAsync(new FailedProvider(error), ProfileType.Microsoft,
            new AuthenticationRequest { RefreshFailureHandler = callback },
            new McProfile { ProfileType = ProfileType.Microsoft, AccessToken = "cached-test" }, token);

    private sealed class FailedProvider(Exception error) : IAuthenticateProvider
    {
        public Task<AuthenticationResult> RefreshAsync(McProfile profile, CancellationToken token) =>
            Task.FromException<AuthenticationResult>(error);
        public Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken token) =>
            throw new AssertFailedException("Refresh policy must not silently start a new authentication.");
        public Task<bool> ValidateAsync(McProfile profile, CancellationToken token) => throw new NotSupportedException();
    }
}
