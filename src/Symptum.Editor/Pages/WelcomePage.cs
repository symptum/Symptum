using Microsoft.UI.Text;
using Symptum.Editor.ViewModels;

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
                            new Image()
                                .Width(32)
                                .Height(32)
                                .Source("ms-appx:///Assets/Images/Symptum Editor.png"),
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
                                    new Button().Content("New")
                                        .Command(MainViewModel.Instance.AddNewItemCommand)
                                        .HorizontalAlignment(HorizontalAlignment.Stretch),
                                    new Button().Content("Open File(s)")
                                        .Command(MainViewModel.Instance.OpenFileCommand)
                                        .HorizontalAlignment(HorizontalAlignment.Stretch),
                                    new Button().Content("Open Folder")
                                        .Command(MainViewModel.Instance.OpenWorkFolderCommand)
                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                )
                          ));

        IconSource = new BitmapIconSource()
        {
            UriSource = new Uri("ms-appx:///Assets/Images/Symptum Editor.png")
        };
    }
}
