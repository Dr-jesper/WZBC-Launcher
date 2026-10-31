using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Conduit;

namespace PCL.Core.Test.Conduit;

[TestClass]
public sealed class ClubReleaseVersionTest
{
    [TestMethod]
    public void CurrentFormalReleaseStartsAboveLegacyClubBaseline()
    {
        Assert.AreEqual("v1.2.0-wzbc.4", ClubRelease.CurrentTag);
        Assert.AreEqual("zyf", ClubRelease.CurrentCodename);

        var formal = new ClubRelease("v1.1.0", "formal", "", "");
        var legacy = new ClubRelease("conduit-v2.15.1-club.10", "legacy", "", "");

        Assert.IsTrue(formal.IsNewerThan(legacy.Tag));
        Assert.IsFalse(legacy.IsNewerThan(formal.Tag));
        Assert.IsFalse(new ClubRelease("conduit-v2.15.1-beta.1-baseline.20261002", "baseline", "", "")
            .IsNewerThan(ClubRelease.CurrentTag));
        Assert.IsFalse(new ClubRelease("conduit-v99.0.0", "legacy", "", "")
            .IsNewerThan(ClubRelease.CurrentTag));
    }

    [TestMethod]
    public void FormalSeriesUsesSemVerOrdering()
    {
        Assert.IsTrue(new ClubRelease("v1.2.1", "patch", "", "")
            .IsNewerThan(ClubRelease.CurrentTag));
        Assert.IsFalse(new ClubRelease("v1.2.0-wzbc.3", "current", "", "")
            .IsNewerThan(ClubRelease.CurrentTag));
        Assert.IsTrue(new ClubRelease("v1.2.0-wzbc.5", "next preview", "", "")
            .IsNewerThan(ClubRelease.CurrentTag));
        Assert.IsTrue(new ClubRelease("v1.2.0", "stable", "", "")
            .IsNewerThan(ClubRelease.CurrentTag));
        Assert.IsTrue(new ClubRelease("v1.2.1-rc.1", "rc", "", "")
            .IsNewerThan(ClubRelease.CurrentTag));
        Assert.IsTrue(new ClubRelease("v2.15.1", "formal", "", "")
            .IsNewerThan("v2.0.0"));
    }

    [TestMethod]
    public void ParseLatestPrefersSemVerOverPublicationDate()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { draft = false, published_at = "2026-10-03T00:00:00Z", tag_name = "v1.1.1", html_url = ClubCatalog.Repository + "/releases/tag/v1.1.1", name = "stable", body = "" },
            new { draft = false, published_at = "2026-10-04T00:00:00Z", tag_name = "v1.1.1-rc.1", html_url = ClubCatalog.Repository + "/releases/tag/v1.1.1-rc.1", name = "release candidate", body = "" },
            new { draft = false, published_at = "2026-10-05T00:00:00Z", tag_name = "v9.0.0", html_url = "https://github.com/example/other/releases/tag/v9.0.0", name = "external", body = "" },
            new { draft = true, published_at = "2026-10-06T00:00:00Z", tag_name = "v9.0.0", html_url = ClubCatalog.Repository + "/releases/tag/v9.0.0", name = "draft", body = "" }
        });

        Assert.AreEqual("v1.1.1", ClubRelease.ParseLatest(json, ClubCatalog.Repository)!.Tag);
    }

    [TestMethod]
    [DataRow("https://github.com/Dr-jesper/WZBC-Launcher", true)]
    [DataRow("https://github.com/Conduit-Club/SHOU-PCL", false)]
    [DataRow("https://github.com/Conduit-Club/PCL-CE-SHOU", false)]
    [DataRow("https://github.com/Dr-jesper/WZBC-Launcher-fake", false)]
    public void DefaultReleaseSourceAcceptsOnlyWzbc(string repository, bool accepted)
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { draft = false, published_at = "2026-10-20T00:00:00Z", tag_name = ClubRelease.CurrentTag,
                html_url = repository + "/releases/tag/" + ClubRelease.CurrentTag, name = "WZBC preview", body = "" }
        });

        Assert.AreEqual(accepted, ClubRelease.ParseLatest(json) is not null);
    }

    [TestMethod]
    [DataRow("https://github.com/Conduit-Club/SHOU-PCL", true)]
    [DataRow("https://github.com/Conduit-Club/PCL-CE-SHOU", true)]
    [DataRow("https://github.com/Conduit-Club/SHOU-PCL-fake", false)]
    public void ReleaseLinksSupportRepositoryRename(string repository, bool accepted)
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { draft = false, published_at = "2026-10-20T00:00:00Z", tag_name = "v1.2.0",
                html_url = repository + "/releases/tag/v1.2.0", name = "zyf", body = "" }
        });

        Assert.AreEqual(accepted, ClubRelease.ParseLatest(json, ClubCatalog.Repository, ClubCatalog.LegacyRepository) is not null);
        Assert.IsNull(ClubRelease.ParseLatest(json), "WZBC must not offer source-project releases.");
    }
}
