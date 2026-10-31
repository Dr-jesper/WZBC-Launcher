using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Conduit;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Test.Conduit;

[TestClass]
public sealed class GameLaunchEligibilityTest
{
    [TestMethod]
    public void SharedLaunchAndScriptPipelineRetainsEligibilityGuards()
    {
        // Source wiring contract, not an end-to-end UI/game test. The app cannot
        // be loaded in core tests without initializing the desktop runtime.
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LaunchPipelineSource")!;
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd().Replace("\r\n", "\n");
        StringAssert.Contains(source, "private static void McLaunchPrecheck()\n    {\n        GameLaunchEligibility.EnsureCanStart(ProfileService.Current);");
        StringAssert.Contains(source, "private static void EnsureLaunchAccount(bool authenticated)\n    {\n        var profile = ProfileService.Current;\n        GameLaunchEligibility.EnsureCanStart(profile);");
        StringAssert.Contains(source, "private static void McLaunchCustom(ModLoader.LoaderTask<int, int> loader)\n    {\n        EnsureLaunchAccount(authenticated: true);");
        StringAssert.Contains(source, "private static void McLaunchRun(ModLoader.LoaderTask<int, Process> loader)\n    {\n        EnsureLaunchAccount(authenticated: true);");
        Assert.IsTrue(source.IndexOf("McLaunchPrecheck();", StringComparison.Ordinal) <
                      source.IndexOf("ModDownload.DlClientFix(", StringComparison.Ordinal));
        var custom = source.IndexOf("private static void McLaunchCustom(", StringComparison.Ordinal);
        Assert.IsTrue(custom < source.IndexOf("currentLaunchOptions.SaveBatch", custom, StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow(ProfileType.Microsoft)]
    [DataRow(ProfileType.Offline)]
    [DataRow(ProfileType.Authlib)]
    [DataRow(ProfileType.YggdrasilConnect)]
    public void AccountIdentityAndCachedSessionCannotGrantEligibility(ProfileType type)
    {
        foreach (var state in new[] { "valid-cache", "expired", "invalidated", "empty-token", "no-expiry" })
        {
            var profile = new McProfile
            {
                ProfileType = type, ProfileId = "test-account", Uuid = "11111111111111111111111111111111",
                Server = ClubCatalog.SkinAuth,
                AccessToken = state == "empty-token" ? "" : "synthetic-not-an-entitlement",
                ExpiresAt = state == "no-expiry" ? null : DateTimeOffset.UtcNow.AddHours(state == "expired" ? -1 : 1),
                AuthenticationInvalidated = state == "invalidated"
            };
            var before = profile.Clone();
            AssertRejected(() => GameLaunchEligibility.EnsureCanStart(profile));
            Assert.AreEqual(before.AccessToken, profile.AccessToken);
            Assert.AreEqual(before.AuthenticationInvalidated, profile.AuthenticationInvalidated);
            Assert.AreEqual(before.ExpiresAt, profile.ExpiresAt);
        }
    }

    [TestMethod]
    public void MissingAndUnknownAccountsCannotGrantEligibility()
    {
        AssertRejected(() => GameLaunchEligibility.EnsureCanStart(null));
        AssertRejected(() => GameLaunchEligibility.EnsureCanStart(new McProfile { ProfileType = (ProfileType)99 }));
    }

    [TestMethod]
    public void PassingR1DoesNotGrantGameEligibility()
    {
        foreach (var type in new[] { ProfileType.Microsoft, ProfileType.Authlib, ProfileType.YggdrasilConnect, ProfileType.Offline })
        {
            var profile = new McProfile { ProfileType = type, Server = ClubCatalog.SkinAuth };
            foreach (var rules in new[] { new LaunchAccountRules(), new LaunchAccountRules(UseClubAccount: true),
                         new LaunchAccountRules(ServerAddress: "mc.wzbcmc.cn"), new LaunchAccountRules(1),
                         new LaunchAccountRules(2, ClubCatalog.SkinAuth), new LaunchAccountRules(3, ClubCatalog.SkinAuth) })
            {
                if (LaunchAccountPolicy.Evaluate(profile, rules) == LaunchAccountRejection.None)
                    AssertRejected(() => GameLaunchEligibility.EnsureCanStart(profile));
            }
        }
    }

    private static void AssertRejected(Action action)
    {
        try { action(); }
        catch (InvalidOperationException ex)
        {
            StringAssert.Contains(ex.Message, "尚无法核验官方游戏资格");
            return;
        }
        Assert.Fail("Game launch must fail closed without official entitlement verification.");
    }
}
