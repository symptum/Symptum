namespace Symptum.Editor.Controls;

public sealed partial class ListEditorControl : UserControl
{
    private object itemsSource;

    public object ItemsSource
    {
        get => itemsSource;
        set
        {
            itemsSource = value;
            listView.ItemsSource = value;
        }
    }

    private DataTemplate itemTemplate;

    public DataTemplate ItemTemplate
    {
        get => itemTemplate;
        set
        {
            itemTemplate = value;
            listView.ItemTemplate = value;
        }
    }

    public ListEditorControl()
    {
        this.InitializeComponent();
        listView.ItemsSource = itemsSource;
        listView.ItemTemplate = itemTemplate;
    }

    private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int count = listView.SelectedItems.Count;
        deleteItemsButton.IsEnabled = count > 0;
        duplicateItemsButton.IsEnabled = count > 0;
    }

    private void addItemButton_Click(object sender, RoutedEventArgs e)
    {
        AddItemRequested?.Invoke(this, new());
    }

    private void deleteItemsButton_Click(object sender, RoutedEventArgs e)
    {
        if (listView.SelectedItems.Count == 0) return;

        List<object> list = listView.SelectedItems.ToList();
        DeleteItemsRequested?.Invoke(this, new(list));
        listView.SelectedItems.Clear();
        list.Clear();
    }

    private void selectAllButton_Click(object sender, RoutedEventArgs e)
    {
        listView.SelectAll();
    }

    private void duplicateItemsButton_Click(object sender, RoutedEventArgs e)
    {
        if (listView.SelectedItems.Count == 0) return;

        List<object> list = listView.SelectedItems.ToList();
        DuplicateItemsRequested?.Invoke(this, new(list));
        listView.SelectedItems.Clear();
        list.Clear();
    }

    public event EventHandler<ListEditorAddItemRequestedEventArgs> AddItemRequested;

    public event EventHandler<ListEditorDeleteItemsRequested> DeleteItemsRequested;

    public event EventHandler<ListEditorDuplicateItemsRequested> DuplicateItemsRequested;
}

public class ListEditorAddItemRequestedEventArgs : EventArgs
{
}

public class ListEditorDeleteItemsRequested : EventArgs
{
    public List<object> ItemsToDelete { get; private set; }

    public ListEditorDeleteItemsRequested(List<object> itemsToDelete)
    {
        ItemsToDelete = itemsToDelete;
    }
}

public class ListEditorDuplicateItemsRequested : EventArgs
{
    public List<object> ItemsToDuplicate { get; private set; }

    public ListEditorDuplicateItemsRequested(List<object> itemsToDuplicate)
    {
        ItemsToDuplicate = itemsToDuplicate;
    }
}
