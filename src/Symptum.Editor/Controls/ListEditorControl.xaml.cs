namespace Symptum.Editor.Controls;

public sealed partial class ListEditorControl : UserControl
{
    #region Properties

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(ListEditorControl),
            new(string.Empty));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
    DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(object),
        typeof(ListEditorControl),
        new(null));

    public object ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(ListEditorControl),
            new(null));

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
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
