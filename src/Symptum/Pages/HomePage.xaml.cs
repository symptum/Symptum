using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using System.Numerics;

namespace Symptum.Pages;

public sealed partial class HomePage : NavigablePage
{
    private LoadedImageSurface? _surface;
    private SpriteVisual? _spriteVisual;
    private Compositor? _compositor;
    private Visual? _heroVisual;
    private bool _heroLoaded = false;

    public HomePage()
    {
        InitializeComponent();
        Loaded += HomePage_Loaded;
        Unloaded += HomePage_Unloaded;
    }

    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        favorites.ItemsSource = ResourceManager.Resources
            .Where(r => r is PackageResource && r is not Subject);

        _heroVisual = ElementCompositionPreview.GetElementVisual(hero);
        _compositor = _heroVisual.Compositor;
        _surface = LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/Images/Symptum_Hero.png"));
        _surface.LoadCompleted += Surface_LoadCompleted;
        hero.SizeChanged += Hero_SizeChanged;
    }

    private void HomePage_Unloaded(object sender, RoutedEventArgs e)
    {
        favorites.ItemsSource = null;
        _spriteVisual?.Dispose();
        _spriteVisual = null;
        _surface!.LoadCompleted -= Surface_LoadCompleted;
        _surface?.Dispose();
        _surface = null;
        _heroLoaded = false;
        hero.SizeChanged -= Hero_SizeChanged;
    }

    private void Surface_LoadCompleted(LoadedImageSurface sender, LoadedImageSourceLoadCompletedEventArgs args)
    {
        if (args.Status == LoadedImageSourceLoadStatus.Success &&
            _compositor is not null &&
           _spriteVisual is null &&
           _heroVisual is not null)
        {
            var _lgb = _compositor.CreateLinearGradientBrush();
            _lgb.StartPoint = new System.Numerics.Vector2(0.5f, 0f);
            _lgb.EndPoint = new System.Numerics.Vector2(0.5f, 1f);
            _lgb.MappingMode = CompositionMappingMode.Relative;
            _lgb.ColorStops.Add(_compositor.CreateColorGradientStop(0.0f, Colors.White));
            _lgb.ColorStops.Add(_compositor.CreateColorGradientStop(0.65f, Colors.White));
            _lgb.ColorStops.Add(_compositor.CreateColorGradientStop(0.85f, Colors.Transparent));
            _lgb.ColorStops.Add(_compositor.CreateColorGradientStop(1.0f, Colors.Transparent));

            var brush = _compositor.CreateSurfaceBrush();
            brush.Stretch = CompositionStretch.UniformToFill;
            brush.VerticalAlignmentRatio = 1.0f;
            brush.HorizontalAlignmentRatio = 0.5f;
            brush.Surface = _surface;
            var mask = _compositor.CreateMaskBrush();
            mask.Mask = _lgb;
            mask.Source = brush;

            _spriteVisual = _compositor.CreateSpriteVisual();
            _spriteVisual.Brush = mask;
            _spriteVisual.Size = new Vector2((float)hero.ActualWidth, (float)hero.ActualHeight);
            ElementCompositionPreview.SetElementChildVisual(hero, _spriteVisual);
            _heroLoaded = true;
        }
    }

    private void Hero_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_heroLoaded && _spriteVisual is not null)
        {
            _spriteVisual.Size = new Vector2((float)hero.ActualWidth, (float)hero.ActualHeight);
        }
    }
}
