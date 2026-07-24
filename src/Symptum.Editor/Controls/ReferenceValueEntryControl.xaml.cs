using System.Collections.ObjectModel;
using Symptum.Core.Data;
using Symptum.Core.Data.ReferenceValues;

namespace Symptum.Editor.Controls;

public sealed partial class ReferenceValueEntryControl : UserControl
{
    private readonly ObservableCollection<ListEditorItemWrapper<Quantity>> _quantities = [];

    #region Properties

    public static readonly DependencyProperty EntryProperty =
        DependencyProperty.Register(
            nameof(Entry),
            typeof(ReferenceValueEntry),
            typeof(ReferenceValueEntryControl),
            new PropertyMetadata(null, OnEntryPropertyChanged));

    private static void OnEntryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ReferenceValueEntryControl entryControl)
        {
            entryControl._entryLoaded = false;
            entryControl.LoadEntry(e.NewValue as ReferenceValueEntry);
        }
    }

    public ReferenceValueEntry? Entry
    {
        get => (ReferenceValueEntry)GetValue(EntryProperty);
        set => SetValue(EntryProperty, value);
    }

    #endregion

    public ReferenceValueEntryControl()
    {
        InitializeComponent();
        DataContextChanged += ReferenceValueEntryControl_DataContextChanged;
    }

    private void ReferenceValueEntryControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        Entry = (args.NewValue as ListEditorItemWrapper<ReferenceValueEntry>)?.Value;
    }

    private void ReferenceValueEntryControl_Loaded(object? s, RoutedEventArgs e)
    {
        qtLE.ItemsSource = _quantities;
        qtLE.ActionRequested += LE_ActionRequested;
        if (!_entryLoaded) LoadEntry(Entry);
    }

    private void ReferenceValueEntryControl_Unloaded(object? s, RoutedEventArgs e)
    {
        qtLE.ItemsSource = null;
        _quantities.ClearWrapperListSafe();
        qtLE.ActionRequested -= LE_ActionRequested;
        Entry = null;
    }

    private bool _entryLoaded = false;

    private void LoadEntry(ReferenceValueEntry? entry)
    {
        if (_entryLoaded) return;

        titleTB.Text = entry?.Title;
        _quantities.LoadFromList(entry?.Quantities);
        infTB.Text = entry?.Inference;
        remTB.Text = entry?.Remarks;
        expander.Header = entry?.Title;

        _entryLoaded = true;
    }

    private void UpdateEntry()
    {
        if (Entry is ReferenceValueEntry entry)
        {
            expander.Header = entry.Title = titleTB.Text;
            entry.Quantities = _quantities.UnwrapToList();
            entry.Inference = infTB.Text;
            entry.Remarks = remTB.Text;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateEntry();
        expander.IsExpanded = false;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        LoadEntry(Entry);
        expander.IsExpanded = false;
    }

    private void LE_ActionRequested(object? s, ListEditorItemActionRequestedEventArgs e) =>
        ListEditorControl.HandleActionRequired(_quantities, e, () => new());
}
