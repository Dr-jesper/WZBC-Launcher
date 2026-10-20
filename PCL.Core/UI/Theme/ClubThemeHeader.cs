using System.Collections.Generic;
using System.Windows.Media;
using PCL.Core.App;
using PCL.Core.App.IoC;

namespace PCL.Core.UI.Theme;

/// <summary>顶栏独立于正文颜色，浅黄底使用深字，切回旧主题恢复紫底白字。</summary>
public static class ClubThemeHeader
{
    public static IReadOnlyDictionary<string, string> Colors(ColorTheme theme)
    {
        var frog = theme is ColorTheme.Naiwa or ColorTheme.NaiwaYellow;
        var foreground = theme == ColorTheme.Naiwa ? "#394425" : "#4A3517";
        return new Dictionary<string, string>
        {
            ["Header"] = frog ? NaiwaPalette.Yellow : "#601986",
            ["HeaderText"] = frog ? foreground : "#FFFFFF",
            ["HeaderSelected"] = frog ? "#FFF9E8" : "#FFFFFF",
            ["HeaderSelectedText"] = frog ? foreground : "#601986",
            ["HeaderHover"] = frog ? "#E1C746" : "#74269B"
        };
    }

    public static void Apply(ColorTheme theme)
    {
        var resources = Lifecycle.CurrentApplication.Resources;
        foreach (var (suffix, hex) in Colors(theme))
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            resources["ColorObject" + suffix] = color;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            resources["ColorBrush" + suffix] = brush;
        }
    }
}
