using System.Collections.Generic;
using System.Windows.Media;
using PCL.Core.App.IoC;

namespace PCL.Core.UI.Theme;

/// <summary>奶蛙参考图的暖黄、奶油、绿眼睛与橄榄色；黄色版不混入绿色。</summary>
public static class NaiwaPalette
{
    public const string Yellow = "#EFD858";

    public static IReadOnlyDictionary<string, string> Colors(bool dark, bool yellowOnly = false)
    {
        var colors = dark
            ? new Dictionary<string, string>
            {
                ["1"]="#FFF3D1", ["2"]="#F1D978", ["3"]="#ACC56F", ["4"]="#D6B34A",
                ["5"]="#A48F3C", ["6"]="#68613A", ["7"]="#39442B", ["8"]="#303A25",
                ["Gray1"]="#F7F0D9", ["Gray2"]="#E9DEC1", ["Gray3"]="#CCC5A8", ["Gray4"]="#ACA78C",
                ["Gray5"]="#86836D", ["Gray6"]="#565B43", ["Gray7"]="#3E4532", ["Gray8"]="#30382A",
                ["White"]="#242C20", ["Background"]="#1B2119", ["ToolTip"]="#303A25",
                ["Bg0"]="#68613A", ["Bg1"]="#DD303A25", ["TransparentBackground"]="#E61B2119",
                ["SemiWhite"]="#BB242C20", ["HalfWhite"]="#55242C20", ["Transparent"]="#00242C20",
                ["SemiTransparent"]="#01242C20", ["Memory"]="#EFD858"
            }
            : new Dictionary<string, string>
            {
                ["1"]="#394425", ["2"]="#3F5A28", ["3"]="#49652E", ["4"]="#B28C20",
                ["5"]="#D7B647", ["6"]="#EFD858", ["7"]="#F6E9AA", ["8"]="#F8F1D8",
                ["Gray1"]="#302F23", ["Gray2"]="#504E3B", ["Gray3"]="#68654F", ["Gray4"]="#86806A",
                ["Gray5"]="#ABA38B", ["Gray6"]="#D4CDB6", ["Gray7"]="#E9E3CE", ["Gray8"]="#F4EEDC",
                ["White"]="#FFF9E8", ["Background"]="#F8F2DE", ["ToolTip"]="#F8F1D8",
                ["Bg0"]="#E5D8B8", ["Bg1"]="#DDF8F1D8", ["TransparentBackground"]="#E6F8F2DE",
                ["SemiWhite"]="#BBFFF9E8", ["HalfWhite"]="#55FFF9E8", ["Transparent"]="#00FFF9E8",
                ["SemiTransparent"]="#01FFF9E8", ["Memory"]="#806119"
            };
        if (!yellowOnly) return colors;

        // Retain the frog's warm yellow while keeping the rest of this option neutral.
        var neutral = dark
            ? new Dictionary<string, string>
            {
                ["1"]="#FFF4CF", ["2"]="#F3D76A", ["3"]="#E2C35C", ["4"]="#D5B348",
                ["5"]="#A48B3B", ["6"]="#6C5A2E", ["7"]="#443A25", ["8"]="#363022",
                ["Gray1"]="#F5F1E7", ["Gray2"]="#E3DDD0", ["Gray3"]="#C5BFB1", ["Gray4"]="#A59D8D",
                ["Gray5"]="#837B6B", ["Gray6"]="#575044", ["Gray7"]="#3F392E", ["Gray8"]="#302B23",
                ["White"]="#29251E", ["Background"]="#1D1B17", ["ToolTip"]="#363022",
                ["Bg0"]="#6C5A2E", ["Bg1"]="#DD363022", ["TransparentBackground"]="#E61D1B17",
                ["SemiWhite"]="#BB29251E", ["HalfWhite"]="#5529251E", ["Transparent"]="#0029251E",
                ["SemiTransparent"]="#0129251E", ["Memory"]="#EFD858"
            }
            : new Dictionary<string, string>
            {
                ["1"]="#4A3517", ["2"]="#6D4D0D", ["3"]="#785614", ["4"]="#B18B1F",
                ["5"]="#D8B647", ["6"]="#EFD858", ["7"]="#F7E7A0", ["8"]="#FCF2CC",
                ["Gray1"]="#302C24", ["Gray2"]="#514B3E", ["Gray3"]="#6A6253", ["Gray4"]="#89806D",
                ["Gray5"]="#ACA28D", ["Gray6"]="#D8D0BE", ["Gray7"]="#EEE8DB", ["Gray8"]="#F5F1E7",
                ["White"]="#FFFCF5", ["Background"]="#F8F6EE", ["ToolTip"]="#FCF2CC",
                ["Bg0"]="#EFD858", ["Bg1"]="#DDFCF2CC", ["TransparentBackground"]="#E6F8F6EE",
                ["SemiWhite"]="#BBFFFCF5", ["HalfWhite"]="#55FFFCF5", ["Transparent"]="#00FFFCF5",
                ["SemiTransparent"]="#01FFFCF5", ["Memory"]="#806119"
            };
        return neutral;
    }

    public static void Apply(bool dark, bool yellowOnly)
    {
        var resources = Lifecycle.CurrentApplication.Resources;
        foreach (var (suffix, hex) in Colors(dark, yellowOnly))
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            resources["ColorObject" + suffix] = color;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            resources["ColorBrush" + suffix] = brush;
        }
    }
}
