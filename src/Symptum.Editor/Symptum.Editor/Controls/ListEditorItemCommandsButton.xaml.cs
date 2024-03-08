using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Symptum.Editor.Controls;

[ContentProperty(Name = nameof(ItemContent))]
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

    public static readonly DependencyProperty ItemContentProperty =
        DependencyProperty.Register(
            nameof(ItemContent),
            typeof(FrameworkElement),
            typeof(ListEditorItemCommandsButton),
            new(null, OnItemContentChanged));

    private static void OnItemContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListEditorItemCommandsButton button)
        {
            button.contentPresenter.Content = (FrameworkElement)e.NewValue;
        }
    }

    public FrameworkElement ItemContent
    {
        get => (FrameworkElement)GetValue(ItemContentProperty);
        set => SetValue(ItemContentProperty, value);
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
        var ir = VisualTreeHelper.GetParent(this);
        var gr = VisualTreeHelper.GetParent(ir);
        var le = VisualTreeHelper.GetParent(gr);
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
