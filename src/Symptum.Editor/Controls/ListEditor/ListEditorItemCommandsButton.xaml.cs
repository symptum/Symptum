using Microsoft.UI.Xaml.Markup;

namespace Symptum.Editor.Controls;

[ContentProperty(Name = nameof(ItemContentTemplate))]
public sealed partial class ListEditorItemCommandsButton : UserControl
{
    #region Properties

    public static readonly DependencyProperty ItemWrapperProperty =
        DependencyProperty.Register(
            nameof(ItemWrapper),
            typeof(object),
            typeof(ListEditorItemCommandsButton),
            new(null, OnItemWrapperChanged));

    private static void OnItemWrapperChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListEditorItemCommandsButton button)
        {
            button.SetUpCommandParams(e.NewValue);
            button.contentPresenter.Content = e.NewValue;
        }
    }

    public object ItemWrapper
    {
        get => GetValue(ItemWrapperProperty);
        set => SetValue(ItemWrapperProperty, value);
    }

    public static readonly DependencyProperty ListEditorProperty =
        DependencyProperty.Register(
            nameof(ListEditor),
            typeof(ListEditorControl),
            typeof(ListEditorItemCommandsButton),
            new(null, OnListEditorChanged));

    private static void OnListEditorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListEditorItemCommandsButton button && e.NewValue is ListEditorControl listEditor)
        {
            button.SetUpCommands(listEditor);
        }
    }

    public ListEditorControl ListEditor
    {
        get => (ListEditorControl)GetValue(ListEditorProperty);
        set => SetValue(ListEditorProperty, value);
    }

    public static readonly DependencyProperty ItemContentTemplateProperty =
        DependencyProperty.Register(
            nameof(ItemContentTemplate),
            typeof(DataTemplate),
            typeof(ListEditorItemCommandsButton),
            new(null, OnItemContentChanged));

    private static void OnItemContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListEditorItemCommandsButton button)
        {
            button.contentPresenter.ContentTemplate = (DataTemplate)e.NewValue;
        }
    }

    public DataTemplate ItemContentTemplate
    {
        get => (DataTemplate)GetValue(ItemContentTemplateProperty);
        set => SetValue(ItemContentTemplateProperty, value);
    }

    #endregion

    public ListEditorItemCommandsButton()
    {
        InitializeComponent();
        Loaded += ListEditorItemCommandsButton_Loaded;
        Unloaded += ListEditorItemCommandsButton_Unloaded;
    }

    private void ListEditorItemCommandsButton_Unloaded(object sender, RoutedEventArgs e)
    {
        ListEditor = null;
        deleteItemBtn.Command = null;
        deleteItemBtn.CommandParameter = null;
        duplicateItemBtn.Command = null;
        duplicateItemBtn.CommandParameter = null;
        moveItemUpBtn.Command = null;
        moveItemUpBtn.CommandParameter = null;
        moveItemDownBtn.Command = null;
        moveItemDownBtn.CommandParameter = null;
    }

    private void ListEditorItemCommandsButton_Loaded(object sender, RoutedEventArgs e)
    {
        var ir = VisualTreeHelper.GetParent(this); // ItemsRepeater
        var gr = VisualTreeHelper.GetParent(ir); // Grid
        var le = VisualTreeHelper.GetParent(gr); // ListEditor
        ListEditor = le as ListEditorControl;
    }

    private void SetUpCommands(ListEditorControl listEditor)
    {
        if (listEditor != null)
        {
            deleteItemBtn.Command = listEditor.RemoveItemCommand;
            duplicateItemBtn.Command = listEditor.DuplicateItemCommand;
            moveItemUpBtn.Command = listEditor.MoveItemUpCommand;
            moveItemDownBtn.Command = listEditor.MoveItemDownCommand;
        }
    }

    private void SetUpCommandParams(object itemWrapper)
    {
        if (itemWrapper != null)
        {
            deleteItemBtn.CommandParameter = itemWrapper;
            duplicateItemBtn.CommandParameter = itemWrapper;
            moveItemUpBtn.CommandParameter = itemWrapper;
            moveItemDownBtn.CommandParameter = itemWrapper;
        }
    }
}
