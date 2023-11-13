using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Symptum.Editor.Controls
{
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

        private void addItemButton_Click(object sender, RoutedEventArgs e)
        {
            AddItemRequested?.Invoke(this, new ListEditorAddItemRequestedEventArgs());
        }

        private void deleteItemsButton_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItems.Count == 0) return;

            DeleteItemsRequested?.Invoke(this, new ListEditorDeleteItemsRequested(listView.SelectedItems.ToList()));
            listView.SelectedItems.Clear();
        }

        private void selectAllButton_Click(object sender, RoutedEventArgs e)
        {
            listView.SelectAll();
        }

        public event EventHandler<ListEditorAddItemRequestedEventArgs> AddItemRequested;

        public event EventHandler<ListEditorDeleteItemsRequested> DeleteItemsRequested;
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
}
