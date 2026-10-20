using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Conduit;

namespace PCL.Core.Test.Conduit;

[TestClass]
public sealed class ClubBrandTest
{
    [TestMethod]
    public void NewGameTypeInfoUsesClubBrand()
    {
        Assert.AreEqual("WZBC MC", ClubCatalog.BrandName);
        Assert.AreEqual("WZBC Minecraft Club", ClubCatalog.BrandNameEnglish);
        Assert.AreEqual(ClubCatalog.BrandName, ClubCatalog.GameTypeInfo);
        Assert.AreEqual("WZBC-Launcher", ClubCatalog.LauncherName);
        Assert.AreEqual("WZBC-Launcher", ClubCatalog.LauncherNameEnglish);
    }

    [TestMethod]
    public void WzbcUsesItsOwnReleaseRepository()
    {
        Assert.AreEqual("https://github.com/Dr-jesper/WZBC-Launcher", ClubCatalog.ReleaseRepository);
        Assert.IsFalse(ClubCatalog.UpdateNoticesReady);
        Assert.IsNull(ClubRelease.ParseLatest("[]"));
        Assert.IsNull(ClubRelease.ParseLatest("[]", "http://github.com/owner/repo"));
        Assert.IsNull(ClubRelease.ParseLatest("[]", string.Empty));
    }

    [TestMethod]
    public void LegacyDefaultIsMigratedWithoutTouchingUserValues()
    {
        Assert.AreEqual(ClubCatalog.GameTypeInfo,
            ClubCatalog.MigrateLegacyGameTypeInfo(ClubCatalog.LegacyGameTypeInfo));
        Assert.AreEqual(string.Empty, ClubCatalog.MigrateLegacyGameTypeInfo(string.Empty));
        Assert.AreEqual("我的自定义版本", ClubCatalog.MigrateLegacyGameTypeInfo("我的自定义版本"));
        Assert.AreEqual("pclce", ClubCatalog.MigrateLegacyGameTypeInfo("pclce"));
        Assert.AreEqual(ClubCatalog.GameTypeInfo,
            ClubCatalog.MigrateLegacyGameTypeInfo(ClubCatalog.GameTypeInfo));
    }
}
