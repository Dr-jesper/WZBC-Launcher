using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PCL.Core.App;

namespace PCL;

public partial class ClubBackdrop : UserControl
{
    private static readonly string[] Pictures = ["campus-wide.jpg", "campus-walk.jpg", "campus-court.jpg", "campus-water.jpg"];
    private readonly DispatcherTimer _carousel = new() { Interval = TimeSpan.FromSeconds(15) };
    private readonly BitmapImage[] _images = new BitmapImage[Pictures.Length];
    private int _index;

    public ClubBackdrop()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        Unloaded += (_, _) => _carousel.Stop();
        IsVisibleChanged += (_, _) => { if (IsVisible && IsLoaded) Refresh(); else _carousel.Stop(); };
        _carousel.Tick += (_, _) => ShowNext();
    }

    private void Refresh()
    {
        Visibility = Config.Preference.Background.ClubHomeSlideshow ? Visibility.Visible : Visibility.Collapsed;
        if (!IsVisible) { _carousel.Stop(); return; }
        CurrentImage.Source ??= Picture(_index);
        _carousel.Start();
    }

    private BitmapImage Picture(int index)
    {
        if (_images[index] is { } cached) return cached;
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri($"pack://application:,,,/Plain Craft Launcher 2;component/Images/Wzbc/{Pictures[index]}");
        image.DecodePixelWidth = 1600;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return _images[index] = image;
    }

    private void ShowNext()
    {
        PreviousImage.Source = CurrentImage.Source;
        _index = (_index + 1) % Pictures.Length;
        CurrentImage.Source = Picture(_index);
        CurrentImage.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1.2)));
    }
}
