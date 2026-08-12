using Microsoft.UI.Xaml.Markup;
using Symptum.Common.Helpers;
using Symptum.Core.Management.Navigation;

namespace Symptum.Pages;

[ContentProperty(Name = nameof(Child))]
public abstract class NavigablePage : Page
{
    private Grid? _grid;
    private TextBlock? _titleTB;
    private static readonly Style? _defaultTitleStyle = (Style?)Application.Current.Resources["PageTitleTextBlockStyle"];
    private static readonly Style? _mediumTitleStyle = (Style?)Application.Current.Resources["MediumPageTitleTextBlockStyle"];
    private static readonly Style? _shortTitleStyle = (Style?)Application.Current.Resources["ShortPageTitleTextBlockStyle"];

    protected Thickness ContentMargin = new(24, 0, 24, 24);
    protected Thickness NarrowContentMargin = new(12, 0, 12, 12);

    public NavigablePage()
    {
        InitializeComponent();
    }

    #region Properties

    #region Title

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(NavigablePage),
            new PropertyMetadata(string.Empty, OnTitlePropertyChanged));

    private static void OnTitlePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigablePage page)
        {
            page._titleTB?.Text = e.NewValue as string ?? string.Empty;
        }
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    #endregion

    #region ShowTitle

    public static readonly DependencyProperty ShowTitleProperty =
        DependencyProperty.Register(
            nameof(ShowTitle),
            typeof(bool),
            typeof(NavigablePage),
            new PropertyMetadata(true, OnShowTitlePropertyChanged));

    private static void OnShowTitlePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigablePage page)
        {
            page._titleTB?.Visibility = e.NewValue is bool value && value ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool ShowTitle
    {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    #endregion

    #region Child

    public static readonly DependencyProperty ChildProperty =
        DependencyProperty.Register(
            nameof(Child),
            typeof(FrameworkElement),
            typeof(NavigablePage),
            new PropertyMetadata(null, OnChildPropertyChanged));

    private static void OnChildPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigablePage page)
        {
            page.UpdateChildren();
        }
    }

    public FrameworkElement? Child
    {
        get => GetValue(ChildProperty) as FrameworkElement;
        set => SetValue(ChildProperty, value);
    }

    #endregion

    #region Navigable

    public static readonly DependencyProperty NavigableProperty =
        DependencyProperty.Register(
            nameof(Navigable),
            typeof(INavigable),
            typeof(NavigablePage),
            new PropertyMetadata(null, OnNavigablePropertyChanged));

    private static void OnNavigablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigablePage navigablePage)
        {
            INavigable? navigable = e.NewValue as INavigable;
            navigablePage.OnNavigableChanged(navigable);
            navigablePage.Title = navigable?.Title ?? string.Empty;
        }
    }

    public INavigable? Navigable
    {
        get => (INavigable?)GetValue(NavigableProperty);
        set => SetValue(NavigableProperty, value);
    }

    #endregion

    #endregion


    protected virtual void OnNavigableChanged(INavigable? navigable)
    { }

    protected virtual void OnLayoutChanged(bool isWide)
    { }

    protected virtual void OnLayoutChanged(int widthLevel, int heightLevel)
    { }

    private void InitializeComponent()
    {
        _titleTB = new TextBlock
        {
            Visibility = Visibility.Visible,
            Style = _defaultTitleStyle
        };

        _grid = new()
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        Content = _grid;

        UpdateChildren();
        Loaded += NavigablePage_Loaded;
        Unloaded += NavigablePage_Unloaded;
    }

    private void NavigablePage_Loaded(object sender, RoutedEventArgs e)
    {
        var win = WindowHelper.MainWindow;
        if (win != null)
        {
            UpdateLayout(win.Bounds.Width, win.Bounds.Height);
            win.SizeChanged += Window_SizeChanged;
        }
    }

    private void NavigablePage_Unloaded(object sender, RoutedEventArgs e)
    {
        WindowHelper.MainWindow?.SizeChanged -= Window_SizeChanged;
    }

    private void UpdateChildren()
    {
        if (_grid != null)
        {
            _grid.Children.Clear();
            if (_titleTB != null)
            {
                _grid.Children.Add(_titleTB);
                Grid.SetRow(_titleTB, 0);
            }
            if (Child != null)
            {
                _grid.Children.Add(Child);
                Grid.SetRow(Child, 1);
            }
        }
    }

    private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        UpdateLayout(e.Size.Width, e.Size.Height);
    }

    private int _widthLevel;
    private bool _isWide;
    private int _heightLevel;

    private void UpdateLayout(double width, double height)
    {
        int widthLevel = width switch
        {
            > 640 => 2,
            > 360 => 1,
            _ => 0
        };
        bool isWide = widthLevel == 2;
        int heightLevel = height switch
        {
            > 560 => 2, // Tall
            > 360 => 1, // Medium
            _ => 0     // Short
        };

        if (ShowTitle && (isWide != _isWide || heightLevel != _heightLevel))
            UpdateTitleStyle(isWide, heightLevel);

        if (widthLevel != _widthLevel || heightLevel != _heightLevel)
            OnLayoutChanged(widthLevel, heightLevel);

        if (isWide != _isWide)
            OnLayoutChanged(isWide);

        _widthLevel = widthLevel;
        _isWide = isWide;
        _heightLevel = heightLevel;
    }

    private void UpdateTitleStyle(bool isWide, int heightLevel)
    {
        if (_titleTB == null)
            return;

        if (!isWide)
            _titleTB.Visibility = Visibility.Collapsed;
        else if (ShowTitle)
        {
            _titleTB.Visibility = Visibility.Visible;
            _titleTB.Style = heightLevel switch
            {
                2 => _defaultTitleStyle,
                1 => _mediumTitleStyle,
                _ => _shortTitleStyle
            };
        }
    }
}
