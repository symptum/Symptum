using Symptum.Editor.ViewModels;
using static Symptum.UI.IconHelper;

namespace Symptum.Editor.Pages;

public sealed partial class WelcomePage : EditorPageBase
{
    Grid? _grid;
    StackPanel? _recents;

    public WelcomePage()
    {
        Title = "Welcome";

        _grid = new Grid()
                .RowDefinitions("Auto,Auto,*")
                .RowSpacing(24)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Children(
                  new TextBlock()
                      .Text(App.AppName)
                      .TextTrimming(TextTrimming.CharacterEllipsis)
                      .TextWrapping(TextWrapping.WrapWholeWords)
                      .HorizontalAlignment(HorizontalAlignment.Stretch)
                      .Style(ThemeResource.Get<Style>("TitleTextBlockStyle")),
                  new StackPanel()
                      .Grid(row: 1)
                      .HorizontalAlignment(HorizontalAlignment.Stretch)
                      .Orientation(Orientation.Vertical)
                      .Spacing(8)
                      .Children(
                          new TextBlock().Text("Start"),
                          IconButton(DocumentIconSource, "New", MainViewModel.Instance.AddNewItemCommand),
                          IconButton(OpenFileIconSource, "Open File(s)", MainViewModel.Instance.OpenFileCommand),
                          IconButton(OpenFolderIconSource, "Open Folder", MainViewModel.Instance.OpenWorkFolderCommand)
                      ));

        if (MainViewModel.Instance.RecentItems.Count > 0)
        {
            _recents = new StackPanel()
                       .Grid(row: 1, column: 1)
                       .HorizontalAlignment(HorizontalAlignment.Stretch)
                       .Orientation(Orientation.Vertical)
                       .Spacing(8)
                       .Children(new TextBlock().Text("Recents"))
                       .Children([..MainViewModel.Instance.RecentItems.Select(i => new HyperlinkButton()
                           {
                               Content = new TextBlock()
                                         .Text(GetItemName(i) ?? string.Empty)
                                         .TextTrimming(TextTrimming.CharacterEllipsis)
                                         .TextWrapping(TextWrapping.NoWrap)
                                         .ToolTipService(toolTip: i),
                               Command = MainViewModel.Instance.OpenRecentItemCommand,
                               CommandParameter = i,
                           })]);
            _grid.Children.Add(_recents);
        }

        this.Content(new ScrollViewer()
                     .Content(_grid));

        IconSource = new BitmapIconSource()
        {
            UriSource = new("ms-appx:///Assets/Images/Symptum_Editor_Monochrome.png")
        };

        SizeChanged += (s, e) =>
        {
            UpdateLayout(e.NewSize.Width > 800);
        };
        UpdateLayout(Width > 800);
    }

    bool _isWide = true;

    private void UpdateLayout(bool isWide)
    {
        if (isWide == _isWide || _grid == null) return;

        _grid.ColumnDefinitions.Clear();

        _isWide = isWide;
        if (isWide)
        {
            _grid.Margin(24)
                 .VerticalAlignment(VerticalAlignment.Center);
            if (_recents != null)
            {
                _grid.ColumnDefinitions("Auto,*")
                     .ColumnSpacing(24);
                _recents?.Grid(row: 1, column: 1);
            }
        }
        else
        {
            _grid.Margin(12)
                 .VerticalAlignment(VerticalAlignment.Stretch)
                 .ColumnSpacing(0);
            _recents?.Grid(row: 2, column: 0);
        }
    }

    private static string? GetItemName(string path)
    {
        for (int i = path.Length - 1; i >= 0; i--)
        {
            if (path[i] ==  System.IO.Path.DirectorySeparatorChar && i < path.Length - 1)
            {
                return path[(i + 1)..];
            }
        }
        return path;
    }
    private Button IconButton(IconSource icon, string content, ICommand? command) =>
        new Button().HorizontalAlignment(HorizontalAlignment.Stretch)
            .Command(command)
            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
            .Content(
            new Grid().ColumnDefinitions("Auto,*")
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .ColumnSpacing(8)
                .Children(
                    new IconSourceElement().IconSource(icon),
                    new TextBlock().Text(content).Grid(column: 1)
                        .HorizontalAlignment(HorizontalAlignment.Left)
                )
        );
}
