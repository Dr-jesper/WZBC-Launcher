using System.Collections.Generic;
using System.Windows.Media;
using PCL.Core.App.IoC;

namespace PCL.Core.UI.Theme;

/// <summary>WZBC 官网品牌紫 #601986，配合纸白与深紫背景。</summary>
public static class WzbcPalette
{
    public static IReadOnlyDictionary<string, string> Colors(bool dark) => dark
        ? new Dictionary<string, string>
        {
            ["1"]="#F6F1F8", ["2"]="#D9B3ED", ["3"]="#C8AFD5", ["4"]="#AF7FC9",
            ["5"]="#8C54A9", ["6"]="#601986", ["7"]="#3B2047", ["8"]="#302037",
            ["Gray1"]="#F6F1F8", ["Gray2"]="#E3D7E9", ["Gray3"]="#C8B8D1", ["Gray4"]="#AA99B4",
            ["Gray5"]="#897892", ["Gray6"]="#56465F", ["Gray7"]="#3D2D46", ["Gray8"]="#302037",
            ["White"]="#291D30", ["Background"]="#1F1624", ["ToolTip"]="#302037",
            ["Bg0"]="#601986", ["Bg1"]="#DD302037", ["TransparentBackground"]="#E61F1624",
            ["SemiWhite"]="#BB291D30", ["HalfWhite"]="#55291D30", ["Transparent"]="#00291D30",
            ["SemiTransparent"]="#01291D30", ["Memory"]="#D9B3ED"
        }
        : new Dictionary<string, string>
        {
            ["1"]="#30113F", ["2"]="#601986", ["3"]="#7426A0", ["4"]="#9161AD",
            ["5"]="#B08CC3", ["6"]="#D5BEDF", ["7"]="#E9DDEF", ["8"]="#F1E9F5",
            ["Gray1"]="#211C24", ["Gray2"]="#493D50", ["Gray3"]="#655D69", ["Gray4"]="#817289",
            ["Gray5"]="#A99BB1", ["Gray6"]="#D5CFD7", ["Gray7"]="#E8E1EB", ["Gray8"]="#F1ECF3",
            ["White"]="#FCFAFD", ["Background"]="#F7F5F0", ["ToolTip"]="#F1E9F5",
            ["Bg0"]="#C8AFD5", ["Bg1"]="#DDF1E9F5", ["TransparentBackground"]="#E6F7F5F0",
            ["SemiWhite"]="#BBFCFAFD", ["HalfWhite"]="#55FCFAFD", ["Transparent"]="#00FCFAFD",
            ["SemiTransparent"]="#01F1E9F5", ["Memory"]="#601986"
        };

    public static void Apply(bool dark)
    {
        var resources = Lifecycle.CurrentApplication.Resources;
        foreach (var (suffix, hex) in Colors(dark))
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            resources["ColorObject" + suffix] = color;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            resources["ColorBrush" + suffix] = brush;
        }
    }
}
