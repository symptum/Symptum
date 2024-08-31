using System.Collections.ObjectModel;
using Symptum.Core.Data;
using Symptum.Core.Data.ReferenceValues;

namespace Symptum.Editor.Controls;

public sealed partial class ReferenceValueEntryControl : UserControl
{
    private readonly ObservableCollection<ListEditorItemWrapper<Quantity>> _quantities = [];

    public ReferenceValueEntryControl()
    {
        InitializeComponent();

        HandleListEditors();
    }

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
            entryControl.LoadEntry(e.NewValue as ReferenceValueEntry);
        }
    }

    public ReferenceValueEntry? Entry
    {
        get => (ReferenceValueEntry)GetValue(EntryProperty);
        set => SetValue(EntryProperty, value);
    }

    #endregion

    private void LoadEntry(ReferenceValueEntry? entry)
    {
        titleTB.Text = entry?.Title;
        _quantities.LoadFromList(entry?.Quantities);
        infTB.Text = entry?.Inference;
        remTB.Text = entry?.Remarks;
        expander.Header = entry?.Title;
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

    private void HandleListEditors()
    {
        qtLE.ItemsSource = _quantities;
        qtLE.AddItemRequested += (s, e) => _quantities.Add(new ListEditorItemWrapper<Quantity>(new()));
        qtLE.ClearItemsRequested += (s, e) => _quantities.Clear();
        qtLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<Quantity> data)
                _quantities.Remove(data);
        };
        qtLE.DuplicateItemRequested += (s, e) =>
        {
            //if (e is ListEditorItemWrapper<Quantity> data)
            //    _quantities.Add(new() { Value = data.Value });
        };
        qtLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<Quantity> data)
            {
                int oldIndex = _quantities.IndexOf(data);
                int newIndex = Math.Max(oldIndex - 1, 0);
                _quantities.Move(oldIndex, newIndex);
            }
        };
        qtLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<Quantity> data)
            {
                int oldIndex = _quantities.IndexOf(data);
                int newIndex = Math.Min(oldIndex + 1, _quantities.Count - 1);
                _quantities.Move(oldIndex, newIndex);
            }
        };
    }

    private void okButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateEntry();
        expander.IsExpanded = false;
    }

    private void cancelButton_Click(object sender, RoutedEventArgs e)
    {
        LoadEntry(Entry);
        expander.IsExpanded = false;
    }
}
