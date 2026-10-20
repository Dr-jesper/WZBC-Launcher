using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.App;
using PCL.Core.UI.Theme;

namespace PCL.Core.Test.Conduit;

[TestClass]
public sealed class NaiwaPaletteTest
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void BodyAndHighlightedTextRemainReadable(bool dark, bool yellowOnly)
    {
        var colors = NaiwaPalette.Colors(dark, yellowOnly);
        foreach (var (text, surface) in new[]
                 { ("Gray1", "Background"), ("Gray1", "White"), ("2", "8"), ("1", "ToolTip"), ("3", "White") })
            AssertContrast(colors[text], colors[surface], $"dark={dark}, yellow={yellowOnly}: {text}/{surface}");
        AssertContrast(colors["8"], colors["3"], "Highlighted tab text");
    }

    [TestMethod]
    [DataRow(ColorTheme.Naiwa)]
    [DataRow(ColorTheme.NaiwaYellow)]
    [DataRow(ColorTheme.Wzbc)]
    public void TitleTabsRemainReadableInNormalHoverAndSelectedStates(ColorTheme theme)
    {
        var header = ClubThemeHeader.Colors(theme);
        AssertContrast(header["HeaderText"], header["Header"], "Title");
        AssertContrast(header["HeaderText"], header["HeaderHover"], "Title hover");
        AssertContrast(header["HeaderSelectedText"], header["HeaderSelected"], "Title selected");
    }

    [TestMethod]
    public void BothModesOverrideAllClubSurfaceResourcesWhenSwitchingThemes()
    {
        foreach (var dark in new[] { false, true })
            foreach (var yellowOnly in new[] { false, true })
                CollectionAssert.AreEquivalent(WzbcPalette.Colors(dark).Keys.ToArray(),
                    NaiwaPalette.Colors(dark, yellowOnly).Keys.ToArray());
    }

    private static void AssertContrast(string a, string b, string context)
    {
        static double Luminance(string color)
        {
            var channels = Enumerable.Range(0, 3)
                .Select(i => Convert.ToInt32(color.Substring(1 + i * 2, 2), 16) / 255d)
                .Select(c => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4)).ToArray();
            return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
        }
        var left = Luminance(a);
        var right = Luminance(b);
        Assert.IsGreaterThanOrEqualTo(4.5, (Math.Max(left, right) + 0.05) / (Math.Min(left, right) + 0.05), context);
    }
}
