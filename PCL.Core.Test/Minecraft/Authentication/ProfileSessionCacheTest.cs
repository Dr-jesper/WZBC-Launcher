using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Conduit;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Authentication;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Test.Minecraft.Authentication;

[TestClass]
public sealed class ProfileSessionCacheTest
{
    [TestMethod]
    public void UnexpiredUpstreamSessionRemainsReusableForTheSameAccount()
    {
        var now = DateTimeOffset.UtcNow;
        var profile = Microsoft(now);
        Assert.IsTrue(ProfileService.CanReuseCachedSession(ProfileType.Microsoft,
            new() { ExpectedProfileId = profile.ProfileId }, profile, now));
        Assert.IsFalse(ProfileService.CanReuseCachedSession(ProfileType.Microsoft,
            new() { ExpectedProfileId = "account-b" }, profile, now));
    }

    [TestMethod]
    public void ExpiredMissingOrForcedSessionsNeverTakeTheFastPath()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var change in new Action<McProfile>[] { p => p.AccessToken = "", p => p.Uuid = "", p => p.ProfileId = "", p => p.AuthenticationInvalidated = true,
                     p => p.ExpiresAt = null, p => p.ExpiresAt = now.AddSeconds(30), p => p.ExpiresAt = now.AddMinutes(-1) })
        {
            var profile = Microsoft(now);
            change(profile);
            Assert.IsFalse(ProfileService.CanReuseCachedSession(ProfileType.Microsoft, new(), profile, now));
        }
        Assert.IsFalse(ProfileService.CanReuseCachedSession(ProfileType.Microsoft, new() { ForceRefresh = true }, Microsoft(now), now));
        Assert.IsFalse(ProfileService.CanReuseCachedSession(ProfileType.Microsoft, new() { ForceReselectProfile = true }, Microsoft(now), now));
    }

    [TestMethod]
    public void ThirdPartySessionCannotMigrateToAnotherProviderOrRole()
    {
        var profile = Microsoft(DateTimeOffset.UtcNow);
        profile.ProfileType = ProfileType.YggdrasilConnect;
        profile.Server = ClubCatalog.SkinAuth;
        profile.DiscoveryAddress = "https://skin.wzbcmc.cn/.well-known/openid-configuration";
        Assert.IsTrue(ProfileService.MatchesExistingAuthentication(ProfileType.YggdrasilConnect,
            new() { Server = profile.Server + "/authserver/", PreferredProfileId = profile.Uuid }, profile));
        Assert.IsFalse(ProfileService.MatchesExistingAuthentication(ProfileType.YggdrasilConnect,
            new() { Server = ClubCatalog.LittleSkinAuth }, profile));
        Assert.IsFalse(ProfileService.MatchesExistingAuthentication(ProfileType.YggdrasilConnect,
            new() { PreferredProfileId = "other-role" }, profile));
        Assert.IsFalse(ProfileService.MatchesExistingAuthentication(ProfileType.YggdrasilConnect,
            new() { DiscoveryAddress = "https://other.example.test/openid" }, profile));
    }

    [TestMethod]
    public void InvalidationSurvivesRestartAndDoesNotInvalidateAnotherAccount()
    {
        var now = DateTimeOffset.UtcNow;
        var rejected = Microsoft(now);
        rejected.AuthenticationInvalidated = true;
        var valid = Microsoft(now);
        valid.ProfileId = "account-b";
        var manager = new ProfileManagement<McProfile>();
        manager.LoadFromString("{\"lastUsed\":-1,\"profiles\":[]}");
        manager.Add(rejected);
        manager.Add(valid);
        var reloaded = new ProfileManagement<McProfile>();
        reloaded.LoadFromString(manager.Serialize());
        Assert.IsFalse(ProfileService.CanReuseCachedSession(ProfileType.Microsoft, new(), reloaded.GetAll()[0], now));
        Assert.IsTrue(ProfileService.CanReuseCachedSession(ProfileType.Microsoft, new(), reloaded.GetAll()[1], now));
        Assert.IsTrue(reloaded.GetAll()[0].Clone().AuthenticationInvalidated);
    }

    [TestMethod]
    public void ReauthorizingAnotherMicrosoftAccountCannotOverwriteTheSelectedRole()
    {
        var existing = Microsoft(DateTimeOffset.UtcNow);
        var wrong = new AuthenticationResult
        {
            ProfileType = ProfileType.Microsoft, UserName = "OtherPlayer", Uuid = "22222222222222222222222222222222",
            AccessToken = "other-account-token"
        };
        try
        {
            ProfileService.EnsureSelectedMicrosoftIdentity(existing, wrong);
            Assert.Fail("Different Microsoft identities must not replace each other.");
        }
        catch (PCL.Core.Minecraft.IdentityModel.IdentityModelAuthenticationException error)
        {
            Assert.AreEqual("account_mismatch", error.Error);
            Assert.AreEqual("account-a", existing.ProfileId);
            Assert.AreEqual("cached-session-test", existing.AccessToken);
        }
        ProfileService.EnsureSelectedMicrosoftIdentity(existing, new AuthenticationResult
        {
            ProfileType = ProfileType.Microsoft, UserName = "RenamedPlayer",
            Uuid = "11111111-1111-1111-1111-111111111111", AccessToken = "refreshed-same-account"
        });
    }

    private static McProfile Microsoft(DateTimeOffset now) => new()
    {
        ProfileId = "account-a", Uuid = "11111111111111111111111111111111", UserName = "Player",
        ProfileType = ProfileType.Microsoft, AccessToken = "cached-session-test", ExpiresAt = now.AddMinutes(20)
    };
}
