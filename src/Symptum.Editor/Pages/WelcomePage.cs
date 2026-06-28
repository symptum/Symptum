using Microsoft.UI.Text;
using Symptum.Editor.ViewModels;
using static Symptum.Editor.Common.DefaultIconSources;

namespace Symptum.Editor.Pages;

public sealed partial class WelcomePage : EditorPageBase
{
    public WelcomePage()
    {
        this.Content(new Grid()
                          .RowDefinitions("*,*")
                          .Padding(12)
                          .RowSpacing(24)
                          .HorizontalAlignment(HorizontalAlignment.Center)
                          .VerticalAlignment(VerticalAlignment.Center)
                          .Children(
                            new TextBlock()
                                .Text(App.AppTitle)
                                .FontWeight(FontWeights.Bold)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Style(ThemeResource.Get<Style>("TitleTextBlockStyle")),
                            new StackPanel()
                                .Grid(row: 1)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Orientation(Orientation.Vertical)
                                .Spacing(12)
                                .Children(
                                    IconButton(DocumentIconSource, "New", MainViewModel.Instance.AddNewItemCommand),
                                    IconButton(OpenFileIconSource, "Open File(s)", MainViewModel.Instance.OpenFileCommand),
                                    IconButton(OpenFolderIconSource, "Open Folder", MainViewModel.Instance.OpenWorkFolderCommand)
                                )
                          ));

        IconSource = new BitmapIconSource()
        {
            UriSource = new("ms-appx:///Assets/Images/Symptum_Editor_Monochrome.png")
        };
    }

    private Button IconButton(IconSource icon, string content, ICommand command) =>
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
