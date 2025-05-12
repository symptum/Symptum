using Symptum.Core.Management.Resources;

namespace Symptum.Editor.Controls;

public sealed partial class CommandPaletteDialog : ContentDialog
{
    #region Properties

    public IResource? SelectedResource { get; set; } = null;

    #endregion

    public CommandPaletteDialog()
    {
        InitializeComponent();
        Opened += (s, e) =>
        {
            commandBox.Text = null;
            SearchItems();
        };

        commandBox.TextChanged += (s, e) =>
        {
            SearchItems(commandBox.Text);
        };

        PrimaryButtonClick += (s, e) =>
        {
            if (commandsLV.SelectedItem is IResource resource)
                SelectedResource = resource;
        };
        CloseButtonClick += (s, e) =>
        {
            SelectedResource = null;
        };
    }

    private void SearchItems(string? queryText = null)
    {
        List<IResource> matches = [.. ResourceManager.Resources];
        if (!string.IsNullOrWhiteSpace(queryText))
        {
            matches = [.. ResourceManager.Resources.Where(x =>
            x.Title?.Contains(queryText, StringComparison.InvariantCultureIgnoreCase) ?? false)];
        }

        commandsLV.ItemsSource = matches;
        commandsLV.SelectedItem = matches.FirstOrDefault();
    }
}
