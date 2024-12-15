using Symptum.Editor.Common;

namespace Symptum.Editor.Controls;

public sealed partial class ListEditorControl : UserControl
{
    public ListEditorControl()
    {
        InitializeComponent();
        HandleTemplateChange();
        AddItemCommand = new RelayCommand<Type>(OnAddItem);
        ClearItemsCommand = new RelayCommand(OnClearItems);
        RemoveItemCommand = new RelayCommand<object>(OnRemoveItem);
        DuplicateItemCommand = new RelayCommand<object>(OnDuplicateItem);
        MoveItemUpCommand = new RelayCommand<object>(OnMoveItemUp);
        MoveItemDownCommand = new RelayCommand<object>(OnMoveItemDown);
    }

    #region Properties

    #region Header

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

    #endregion

    #region ItemsSource

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

    #endregion

    #region ItemTemplate

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(ListEditorControl),
            new(null, OnItemTemplatePropertyChanged));

    private static void OnItemTemplatePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ListEditorControl editorControl)
            editorControl.HandleTemplateChange();
    }

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    #endregion

    #region ItemTemplateSelector

    public static readonly DependencyProperty ItemTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(ItemTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(ListEditorControl),
            new(null, OnItemTemplateSelectorPropertyChanged));

    private static void OnItemTemplateSelectorPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ListEditorControl editorControl)
            editorControl.HandleTemplateChange();
    }

    public DataTemplateSelector ItemTemplateSelector
    {
        get => (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty);
        set => SetValue(ItemTemplateSelectorProperty, value);
    }

    #endregion

    #region HasMixedItems

    public static readonly DependencyProperty HasMixedItemsProperty =
        DependencyProperty.Register(
            nameof(HasMixedItems),
            typeof(bool),
            typeof(ListEditorControl),
            new(false, OnHasMixedItemsPropertyChanged));

    private static void OnHasMixedItemsPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ListEditorControl editorControl)
            editorControl.HandleTemplateChange();
    }

    public bool HasMixedItems
    {
        get => (bool)GetValue(HasMixedItemsProperty);
        set => SetValue(HasMixedItemsProperty, value);
    }

    #endregion

    #region ItemTypes

    public static readonly DependencyProperty ItemTypesProperty =
        DependencyProperty.Register(
            nameof(ItemTypes),
            typeof(IEnumerable<NewItemType>),
            typeof(ListEditorControl),
            new(null));

    public IEnumerable<NewItemType> ItemTypes
    {
        get => (IEnumerable<NewItemType>)GetValue(ItemTypesProperty);
        set => SetValue(ItemTypesProperty, value);
    }

    #endregion

    public ICommand AddItemCommand { get; }

    public ICommand ClearItemsCommand { get; }

    public ICommand RemoveItemCommand { get; }

    public ICommand DuplicateItemCommand { get; }

    public ICommand MoveItemUpCommand { get; }

    public ICommand MoveItemDownCommand { get; }

    #endregion

    public event EventHandler<Type?> AddItemRequested;

    public event EventHandler ClearItemsRequested;

    public event EventHandler<object?> RemoveItemRequested;

    public event EventHandler<object?> DuplicateItemRequested;

    public event EventHandler<object?> MoveItemUpRequested;

    public event EventHandler<object?> MoveItemDownRequested;

    private void HandleTemplateChange()
    {
        itemsRepeater.ItemTemplate = HasMixedItems ? ItemTemplateSelector : ItemTemplate;
    }

    private void OnAddItem(Type? type)
    {
        if (type == null && HasMixedItems)
        {
            itemTypePicker.ShowAt(addItemButton);
            return;
        }

        AddItemRequested?.Invoke(this, type);
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

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is NewItemType type)
        {
            itemTypePicker.Hide();
            OnAddItem(type.Type);
        }
    }
}
