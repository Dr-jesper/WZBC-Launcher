using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Conduit;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Test.Conduit;

[TestClass]
public sealed class LaunchAccountPolicyTest
{
    [TestMethod]
    [DataRow(ProfileType.Microsoft)]
    [DataRow(ProfileType.Offline)]
    [DataRow(ProfileType.Authlib)]
    [DataRow(ProfileType.YggdrasilConnect)]
    public void UpstreamLocalModesDoNotRequireMicrosoftBinding(ProfileType type)
    {
        var profile = Profile(type);
        Assert.AreEqual(LaunchAccountRejection.None, LaunchAccountPolicy.Evaluate(profile, new()));
    }

    [TestMethod]
    [DataRow(ProfileType.Offline, 1, LaunchAccountRejection.RequireMicrosoft)]
    [DataRow(ProfileType.Authlib, 1, LaunchAccountRejection.RequireMicrosoft)]
    [DataRow(ProfileType.Microsoft, 2, LaunchAccountRejection.RequireThirdParty)]
    [DataRow(ProfileType.Offline, 2, LaunchAccountRejection.RequireThirdParty)]
    [DataRow(ProfileType.Offline, 3, LaunchAccountRejection.RequireMicrosoftOrThirdParty)]
    [DataRow(ProfileType.Microsoft, 3, LaunchAccountRejection.None)]
    [DataRow(ProfileType.Authlib, 3, LaunchAccountRejection.None)]
    public void InstanceRequirementsApplyToTheSelectedRole(ProfileType type, int requirement, LaunchAccountRejection expected)
        => Assert.AreEqual(expected, LaunchAccountPolicy.Evaluate(Profile(type), new(requirement, ClubCatalog.SkinAuth)));

    [TestMethod]
    [DataRow("mc.wzbcmc.cn")]
    [DataRow("MC.WZBCMC.CN:25565")]
    [DataRow("mc.wzbcmc.cn.")]
    [DataRow("mc-2.wzbcmc.cn")]
    [DataRow("build.wzbcmc.cn:25566")]
    public void DirectClubServerTargetsCannotSkipTheClubAccountRule(string target)
    {
        Assert.AreEqual(LaunchAccountRejection.ClubAccountRequired,
            LaunchAccountPolicy.Evaluate(Profile(ProfileType.Offline), new(ServerAddress: target)));
        Assert.AreEqual(LaunchAccountRejection.None,
            LaunchAccountPolicy.Evaluate(Profile(ProfileType.Authlib), new(ServerAddress: target)));
    }

    [TestMethod]
    public void ClubAndLabyRulesCannotOverrideInstanceRequirements()
    {
        var thirdParty = Profile(ProfileType.Authlib);
        Assert.AreEqual(LaunchAccountRejection.RequireMicrosoft,
            LaunchAccountPolicy.Evaluate(thirdParty, new(1, UseClubAccount: true)));
        Assert.AreEqual(LaunchAccountRejection.RequireMicrosoft,
            LaunchAccountPolicy.Evaluate(thirdParty, new(UseClubAccount: true, RequiresMicrosoft: true)));
        Assert.AreEqual(LaunchAccountRejection.RequireThirdParty,
            LaunchAccountPolicy.Evaluate(Profile(ProfileType.Microsoft), new(2, RequiresMicrosoft: true)));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void InstanceAndClubConstraintsBothApplyAcrossEntryForms(int requirement)
    {
        // Entry forms converge on either the explicit flag or the resolved target host.
        foreach (var type in new[] { ProfileType.Microsoft, ProfileType.Authlib, ProfileType.YggdrasilConnect, ProfileType.Offline })
        foreach (var customProvider in new[] { false, true })
        foreach (var route in new[] { "local", "flag", "server-card", "instance-server", "custom-event" })
        {
            var profile = Profile(type);
            if (customProvider) profile.Server = "https://custom.example.test/auth";
            var club = route != "local";
            var rules = new LaunchAccountRules(requirement, profile.Server,
                UseClubAccount: route == "flag",
                ServerAddress: route switch
                {
                    "server-card" => "mc.wzbcmc.cn",
                    "instance-server" => "MC-2.WZBCMC.CN:25565",
                    "custom-event" => "build.wzbcmc.cn.",
                    _ => null
                });
            var expected = requirement switch
            {
                1 when type != ProfileType.Microsoft => LaunchAccountRejection.RequireMicrosoft,
                2 when type is not (ProfileType.Authlib or ProfileType.YggdrasilConnect) => LaunchAccountRejection.RequireThirdParty,
                3 when type == ProfileType.Offline => LaunchAccountRejection.RequireMicrosoftOrThirdParty,
                _ when club && (type == ProfileType.Offline || (customProvider && type != ProfileType.Microsoft))
                    => LaunchAccountRejection.ClubAccountRequired,
                _ => LaunchAccountRejection.None
            };
            Assert.AreEqual(expected, LaunchAccountPolicy.Evaluate(profile, rules),
                $"requirement={requirement}, type={type}, customProvider={customProvider}, route={route}");
        }
    }

    [TestMethod]
    [DataRow(ProfileType.Authlib, 2)]
    [DataRow(ProfileType.Authlib, 3)]
    [DataRow(ProfileType.YggdrasilConnect, 2)]
    [DataRow(ProfileType.YggdrasilConnect, 3)]
    public void ClubRoutesCannotSkipInstanceAuthServerMatching(ProfileType type, int requirement)
    {
        foreach (var rules in new[]
                 {
                     new LaunchAccountRules(requirement, "https://other.example.test/auth", UseClubAccount: true),
                     new LaunchAccountRules(requirement, "https://other.example.test/auth", ServerAddress: "mc.wzbcmc.cn")
                 })
            Assert.AreEqual(LaunchAccountRejection.AuthServerMismatch, LaunchAccountPolicy.Evaluate(Profile(type), rules));
    }

    [TestMethod]
    public void ThirdPartyRequirementsMatchExactServerOriginAndPath()
    {
        var profile = Profile(ProfileType.Authlib);
        Assert.AreEqual(LaunchAccountRejection.None,
            LaunchAccountPolicy.Evaluate(profile, new(2, ClubCatalog.SkinAuth + "/authserver/")));
        foreach (var wrong in new[] { "http://skin.wzbcmc.cn/api/yggdrasil", "https://skin.wzbcmc.cn:8443/api/yggdrasil",
                     ClubCatalog.SkinAuth + "-other", "https://skin.wzbcmc.cn.attacker.test/api/yggdrasil", ClubCatalog.SkinAuth + "?override=1" })
            Assert.AreEqual(LaunchAccountRejection.AuthServerMismatch,
                LaunchAccountPolicy.Evaluate(profile, new(2, wrong)), wrong);
    }

    [TestMethod]
    public void CustomThirdPartyProvidersRemainAvailableOutsideClubRoutes()
    {
        var custom = Profile(ProfileType.Authlib);
        custom.Server = "https://custom.example.test/auth";
        Assert.AreEqual(LaunchAccountRejection.None, LaunchAccountPolicy.Evaluate(custom, new(2, custom.Server)));
        Assert.AreEqual(LaunchAccountRejection.ClubAccountRequired,
            LaunchAccountPolicy.Evaluate(custom, new(UseClubAccount: true)));
        Assert.IsFalse(LaunchAccountPolicy.IsClubServer("mc.wzbcmc.cn.attacker.test"));
    }

    [TestMethod]
    public void MissingAndUnknownAccountsFailClosed()
    {
        Assert.AreEqual(LaunchAccountRejection.NoProfile, LaunchAccountPolicy.Evaluate(null, new()));
        Assert.AreEqual(LaunchAccountRejection.UnknownProfileType,
            LaunchAccountPolicy.Evaluate(Profile((ProfileType)99), new()));
    }

    [TestMethod]
    public void SessionSnapshotRejectsAccountRoleServerOrCredentialChanges()
    {
        var profile = Profile(ProfileType.Authlib);
        var snapshot = new LaunchAccountSnapshot(profile);
        Assert.IsTrue(snapshot.MatchesAuthenticated(profile.Clone(), DateTimeOffset.UtcNow));
        Assert.IsTrue(snapshot.MatchesResult(profile.Uuid, "Auth", profile.AccessToken));
        Assert.IsFalse(snapshot.MatchesResult(profile.Uuid, "Microsoft", profile.AccessToken));
        foreach (var change in new Action<McProfile>[] { p => p.ProfileId = "other-account", p => p.Uuid = "other-role",
                     p => p.Server = "https://other.example.test/auth", p => p.AccessToken = "other-session",
                     p => p.ProfileType = ProfileType.Microsoft, p => p.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) })
        {
            var changed = profile.Clone();
            change(changed);
            Assert.IsFalse(snapshot.MatchesAuthenticated(changed, DateTimeOffset.UtcNow));
        }
    }

    [TestMethod]
    public void OfflineNameChangesInvalidateOnlyThatLaunchIdentity()
    {
        var profile = Profile(ProfileType.Offline);
        profile.AccessToken = string.Empty;
        var snapshot = new LaunchAccountSnapshot(profile);
        Assert.IsTrue(snapshot.MatchesAuthenticated(profile, DateTimeOffset.UtcNow));
        Assert.IsTrue(snapshot.MatchesResult(profile.Uuid, "Legacy", profile.Uuid));
        profile.UserName = "OtherPlayer";
        Assert.IsFalse(snapshot.MatchesIdentity(profile));
    }

    private static McProfile Profile(ProfileType type) => new()
    {
        ProfileId = "account-a", Uuid = "11111111111111111111111111111111", UserName = "Player", ProfileType = type,
        Server = ClubCatalog.SkinAuth, AccessToken = "session-test", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(20)
    };
}
