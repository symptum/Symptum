using Symptum.Core.Data;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Markdown.Reference;

namespace Symptum.Editor.Controls;

public sealed partial class MarkdownEditorInsertReferenceDialog : ContentDialog, IEditorDialog
{
    public EditorResult EditResult { get; private set; } = EditorResult.None;

    public string Markdown { get; private set; } = string.Empty;

    private MarkdownFileResource? _resource;
    private List<ReferenceValueGroup> groups = [];
    private List<ReferenceValueParameter> parameters = [];
    private List<ReferenceValueEntry> entries = [];
    private List<Quantity> quantities = [];

    public MarkdownEditorInsertReferenceDialog()
    {
        InitializeComponent();
        Opened += MarkdownEditorInsertReferenceDialog_Opened;
        PrimaryButtonClick += MarkdownEditorInsertReferenceDialog_PrimaryButtonClick;
        CloseButtonClick += MarkdownEditorInsertReferenceDialog_CloseButtonClick;
    }

    private void MarkdownEditorInsertReferenceDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        Markdown = string.Empty;
        EditResult = EditorResult.None;
        UpdatePreview();
    }

    public void SetResource(MarkdownFileResource? resource)
    {
        _resource = resource;
        groups.Clear(); parameters.Clear(); entries.Clear(); quantities.Clear();

        // Focus only on the groups inside the package.
        ReferenceValuesPackage? package = ResourceManager.Resources.FirstOrDefault(p => p is ReferenceValuesPackage) as ReferenceValuesPackage;
        CollectReferenceValueGroups(package?.ChildrenResources ?? [], groups);

        if (groups.Count == 0)
        {
            infoTB.Visibility = Visibility.Visible;
            groupCB.Visibility = Visibility.Collapsed;
            parameterCB.IsEnabled = false;
            entryCB.IsEnabled = false;
            quantityCB.IsEnabled = false;
            return;
        }

        infoTB.Visibility = Visibility.Collapsed;
        groupCB.Visibility = Visibility.Visible;
        groupCB.ItemsSource = groups.Select(g => g.Title).ToList();
        groupCB.SelectedIndex = 0;
    }

    private static void CollectReferenceValueGroups(IReadOnlyList<IResource> resources, List<ReferenceValueGroup> results)
    {
        foreach (var resource in resources)
        {
            if (resource is ReferenceValueGroup group)
            {
                results.Add(group);
            }

            if (resource.ChildrenResources is { Count: > 0 })
            {
                CollectReferenceValueGroups(resource.ChildrenResources, results);
            }
        }
    }

    private void GroupCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int index = groupCB.SelectedIndex;
        parameters.Clear(); entries.Clear(); quantities.Clear();
        parameters = index >= 0 && index < groups.Count ? (groups[index].Parameters?.ToList() ?? []) : [];

        if (parameters.Count == 0)
        {
            parameterCB.ItemsSource = null;
            parameterCB.IsEnabled = false;
            return;
        }

        parameterCB.ItemsSource = parameters.Select(p => p.Title).ToList();
        parameterCB.IsEnabled = true;
        parameterCB.SelectedIndex = 0;
    }

    private void ParameterCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        entries.Clear(); quantities.Clear();
        int index = parameterCB.SelectedIndex;
        entries = index >= 0 && index < parameters.Count ? (parameters[index].Entries ?? []) : [];

        if (entries.Count == 0)
        {
            entryCB.ItemsSource = null;
            entryCB.IsEnabled = false;
            return;
        }

        entryCB.ItemsSource = entries.Select((entry, i) => string.IsNullOrWhiteSpace(entry.Title) ? $"Entry {i + 1}" : entry.Title).ToList();
        entryCB.IsEnabled = true;
        entryCB.SelectedIndex = 0;
    }

    private void EntryCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        quantities.Clear();
        int index = entryCB.SelectedIndex;
        quantities = index >= 0 && index < entries.Count ? (entries[index].Quantities ?? []) : [];

        if (quantities.Count == 0)
        {
            quantityCB.ItemsSource = null;
            quantityCB.IsEnabled = false;
            UpdatePreview();
            return;
        }

        quantityCB.ItemsSource = quantities.Select(q => q.ToString()).ToList();
        quantityCB.IsEnabled = true;
        quantityCB.SelectedIndex = 0;
    }

    private void QuantityCB_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        if (TryGetSelection(out string? parameterId, out int entryIndex, out int quantityIndex))
        {
            Markdown = ReferenceInlineHelper.BuildSyntax(parameterId, entryIndex, quantityIndex);

            string value = string.Empty;
            if (entryIndex >= 0 && entryIndex < entries.Count)
            {
                var entry = entries[entryIndex];
                if (quantityIndex >= 0 && quantityIndex < entry.Quantities?.Count)
                    value = entry.Quantities[quantityIndex].ToString();
            }

            previewTB.Text = $"{Markdown}  \u2192  {value}";
            previewTB.Visibility = Visibility.Visible;
        }
        else
        {
            previewTB.Visibility = Visibility.Collapsed;
        }
    }

    private bool TryGetSelection(out string? parameterId, out int entryIndex, out int quantityIndex)
    {
        parameterId = null;
        entryIndex = 0;
        quantityIndex = 0;

        int parameterIndex = parameterCB.SelectedIndex;
        if (parameterIndex < 0 || parameterIndex >= parameters.Count) return false;

        parameterId = parameters[parameterIndex].Id;
        if (string.IsNullOrWhiteSpace(parameterId)) return false;

        entryIndex = entryCB.SelectedIndex;
        if (entryIndex < 0) entryIndex = 0;
        quantityIndex = quantityCB.SelectedIndex;
        if (quantityIndex < 0) quantityIndex = 0;

        return true;
    }

    private void MarkdownEditorInsertReferenceDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        UpdatePreview();
        if (string.IsNullOrWhiteSpace(Markdown))
        {
            args.Cancel = true;
            return;
        }

        AddSelectedGroupToDependencies();
        EditResult = EditorResult.Create;
    }

    // Adds the selected group to the resource's dependencies allowing value resolution.
    private void AddSelectedGroupToDependencies()
    {
        if (_resource == null) return;

        int index = groupCB.SelectedIndex;
        if (index < 0 || index >= groups.Count) return;

        ReferenceValueGroup group = groups[index];
        if (string.IsNullOrWhiteSpace(group.Id)) return;

        _resource.DependencyIds ??= [];
        if (!_resource.DependencyIds.Contains(group.Id))
            _resource.DependencyIds.Add(group.Id);

        _resource.Dependencies ??= [];
        if (!_resource.Dependencies.Contains(group))
            _resource.Dependencies.Add(group);
    }

    private void MarkdownEditorInsertReferenceDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = EditorResult.Cancel;
    }

    public async Task<EditorResult> InsertAsync()
    {
        await ShowAsync();
        return EditResult;
    }
}
