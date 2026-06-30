using Microsoft.UI.Text;
using Symptum.Editor.Common;
using Symptum.Editor.ViewModels;

namespace Symptum.Editor.Controls;

public sealed partial class OutputControl : UserControl
{
    private readonly Button closeButton;

    public OutputControl()
    {
        closeButton = new Button()
            .Style(ThemeResource.Get<Style>("IconButtonStyle"))
            .Grid(column: 1)
            .ToolTipService(toolTip: "Close Panel")
            .Content(
                new FontIcon()
                    .FontSize(12)
                    .Glyph(CommonGlyphs.Close));
        closeButton.Click += CloseButton_Click;

        this.Content(
            new Grid()
                .RowDefinitions("Auto,*")
                .BorderThickness(0, 1, 0, 0)
                .BorderBrush(ThemeResource.Get<Brush>("AccentFillColorDefaultBrush"))
                .Children(
                    new Grid()
                        .ColumnDefinitions("*,Auto")
                        .Children(
                            new TextBlock()
                            .Text("Output")
                            .Style(ThemeResource.Get<Style>("CaptionTextBlockStyle"))
                            .FontWeight(FontWeights.SemiBold)
                            .VerticalAlignment(VerticalAlignment.Center),
                            closeButton
                        ),
                    new TextBox()
                        .Text(() => MainViewModel.Instance.OutputText)
                        .FontFamily(ThemeResource.Get<FontFamily>("DefaultCodeFontFamily"))
                )
            );
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        MainViewModel.Instance.ShowOutputPanel = false;
    }
}
