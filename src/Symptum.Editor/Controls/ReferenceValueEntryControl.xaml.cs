using System.Collections.ObjectModel;
using Symptum.Core.Data.ReferenceValues;

namespace Symptum.Editor.Controls;

public sealed partial class ReferenceValueEntryControl : UserControl
{
    private readonly ObservableCollection<ListEditorItemWrapper<ReferenceValueData>> _data = [];

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

    public ReferenceValueEntryControl()
    {
        this.InitializeComponent();

        HandleListEditors();
    }

    private void LoadEntry(ReferenceValueEntry? entry)
    {
        titleTB.Text = entry?.Title;
        _data.LoadFromList(entry?.Data);
        infTB.Text = entry?.Inference;
        remTB.Text = entry?.Remarks;
        expander.Header = entry?.Title;
    }

    private void UpdateEntry()
    {
        if (Entry is ReferenceValueEntry entry)
        {
            expander.Header = entry.Title = titleTB.Text;
            entry.Data = _data.UnwrapToList();
            entry.Inference = infTB.Text;
            entry.Remarks = remTB.Text;
        }
    }

    private void HandleListEditors()
    {
        dtLE.ItemsSource = _data;
        dtLE.AddItemRequested += (s, e) => _data.Add(new ListEditorItemWrapper<ReferenceValueData>(new()));
        dtLE.ClearItemsRequested += (s, e) => _data.Clear();
        dtLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<ReferenceValueData> data)
                _data.Remove(data);
        };
        dtLE.DuplicateItemRequested += (s, e) =>
        {
            //if (e is ListEditorItemWrapper<ReferenceValueData> data)
            //    _data.Add(new() { Value = data.Value });
        };
        dtLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<ReferenceValueData> data)
            {
                int oldIndex = _data.IndexOf(data);
                int newIndex = Math.Max(oldIndex - 1, 0);
                _data.Move(oldIndex, newIndex);
            }
        };
        dtLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<ReferenceValueData> data)
            {
                int oldIndex = _data.IndexOf(data);
                int newIndex = Math.Min(oldIndex + 1, _data.Count - 1);
                _data.Move(oldIndex, newIndex);
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
