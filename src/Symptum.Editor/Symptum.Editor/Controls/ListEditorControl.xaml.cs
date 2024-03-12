using System.Collections;

namespace Symptum.Editor.Controls;

public sealed partial class ListEditorControl : UserControl
{
    #region Properties

    private object itemsSource;

    public object ItemsSource
    {
        get => itemsSource;
        set
        {
            itemsSource = value;
            itemsRepeater.ItemsSource = value;
        }
    }

    private DataTemplate itemTemplate;

    public DataTemplate ItemTemplate
    {
        get => itemTemplate;
        set
        {
            itemTemplate = value;
            itemsRepeater.ItemTemplate = value;
        }
    }

    public ICommand AddItemCommand { get; }

    public ICommand ClearItemsCommand { get; }

    public ICommand RemoveItemCommand { get; }

    public ICommand DuplicateItemCommand { get; }

    public ICommand MoveItemUpCommand { get; }

    public ICommand MoveItemDownCommand { get; }

    #endregion

    public ListEditorControl()
    {
        InitializeComponent();
        itemsRepeater.ItemsSource = itemsSource;
        itemsRepeater.ItemTemplate = itemTemplate;
        AddItemCommand = new RelayCommand(OnAddItem);
        ClearItemsCommand = new RelayCommand(OnClearItems);
        RemoveItemCommand = new RelayCommand<object>(OnRemoveItem);
        DuplicateItemCommand = new RelayCommand<object>(OnDuplicateItem);
        MoveItemUpCommand = new RelayCommand<object>(OnMoveItemUp);
        MoveItemDownCommand = new RelayCommand<object>(OnMoveItemDown);
    }

    private void OnAddItem()
    {
        AddItemRequested?.Invoke(this, null);
    }

    private void OnClearItems()
    {
        ClearItemsRequested?.Invoke(this, null);
    }

    private void OnRemoveItem(object? wrapper)
    {
        RemoveItemRequested?.Invoke(this, wrapper);
    }

    private void OnDuplicateItem(object? wrapper)
    {
        DuplicateItemRequested?.Invoke(this, wrapper);
    }

    private void OnMoveItemUp(object? wrapper)
    {
        MoveItemUpRequested?.Invoke(this, wrapper);
    }

    private void OnMoveItemDown(object? wrapper)
    {
        MoveItemDownRequested?.Invoke(this, wrapper);
    }

    public event EventHandler AddItemRequested;

    public event EventHandler ClearItemsRequested;

    public event EventHandler<object?> RemoveItemRequested;

    public event EventHandler<object?> DuplicateItemRequested;

    public event EventHandler<object?> MoveItemUpRequested;

    public event EventHandler<object?> MoveItemDownRequested;
}

public class ListEditorItemWrapper<T>
{
    public T Value { get; set; }

    public ListEditorItemWrapper()
    { }

    public ListEditorItemWrapper(T value)
    {
        Value = value;
    }
}
