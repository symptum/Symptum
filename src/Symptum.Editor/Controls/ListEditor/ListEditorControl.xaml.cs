using System.Collections.ObjectModel;
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

    public event EventHandler<ListEditorItemActionRequestedEventArgs> ActionRequested;

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

        ActionRequested?.Invoke(this, new(ListEditorItemActionType.Add, type));
    }

    private void OnClearItems()
    {
        ActionRequested?.Invoke(this, new(ListEditorItemActionType.Clear));
    }

    private void OnRemoveItem(object? wrapper)
    {
        ActionRequested?.Invoke(this, new(ListEditorItemActionType.Remove, wrapper));
    }

    private void OnDuplicateItem(object? wrapper)
    {
        ActionRequested?.Invoke(this, new(ListEditorItemActionType.Duplicate, wrapper));
    }

    private void OnMoveItemUp(object? wrapper)
    {
        ActionRequested?.Invoke(this, new(ListEditorItemActionType.MoveUp, wrapper));

    }

    private void OnMoveItemDown(object? wrapper)
    {
        ActionRequested?.Invoke(this, new(ListEditorItemActionType.MoveDown, wrapper));

    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is NewItemType type)
        {
            itemTypePicker.Hide();
            OnAddItem(type.Type);
        }
    }

    public static void HandleActionRequired<T>(ObservableCollection<ListEditorItemWrapper<T>> source,
        ListEditorItemActionRequestedEventArgs e, Func<T> createNew, Func<T>? duplicate = null)
    {
        switch (e.ActionType)
        {
            case ListEditorItemActionType.Add:
                {
                    ArgumentNullException.ThrowIfNull(createNew);
                    source.Add(new(createNew()));
                }
                break;
            case ListEditorItemActionType.Clear:
                source.ClearWrapperListSafe();
                break;
            case ListEditorItemActionType.Remove:
                source.RemoveWrapperSafe(e.Arguments as ListEditorItemWrapper<T>);
                break;
            case ListEditorItemActionType.MoveUp:
                source.MoveWrapperUp(e.Arguments as ListEditorItemWrapper<T>);
                break;
            case ListEditorItemActionType.MoveDown:
                source.MoveWrapperDown(e.Arguments as ListEditorItemWrapper<T>);
                break;
            case ListEditorItemActionType.Duplicate:
                if (duplicate != null)
                    source.Add(new(duplicate()));
                break;
            default: break;
        }
    }
}
