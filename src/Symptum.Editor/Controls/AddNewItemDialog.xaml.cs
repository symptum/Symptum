using Symptum.Core.Management.Resources;
using Symptum.Editor.Common;

namespace Symptum.Editor.Controls;

public sealed partial class AddNewItemDialog : ContentDialog
{
    private List<NewItemType>? availItemTypes;

    #region Properties

    public IResource? ParentResource { get; set; } = null;

    public string ItemTitle { get; set; } = string.Empty;

    public Type? SelectedItemType { get; set; } = null;

    public EditorResult Result { get; private set; } = EditorResult.None;

    #endregion

    public AddNewItemDialog()
    {
        InitializeComponent();
        newItemsLV.ItemsSource = NewItemType.KnownTypes;
        Opened += AddNewItemDialog_Opened;
        PrimaryButtonClick += AddNewItemDialog_PrimaryButtonClick;
        CloseButtonClick += AddNewItemDialog_CloseButtonClick;
        queryBox.TextChanged += QueryBox_TextChanged;
        queryBox.QuerySubmitted += QueryBox_QuerySubmitted;
    }

    private void QueryBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        SearchItems(args.QueryText);
    }

    private void QueryBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            SearchItems(sender.Text);
    }

    private void SearchItems(string? queryText)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            newItemsLV.ItemsSource = availItemTypes;
            return;
        }

        List<NewItemType> suitableItems = availItemTypes?.FindAll(x =>
            x.DisplayName?.Contains(queryText, StringComparison.InvariantCultureIgnoreCase) ?? false) ?? [];

        newItemsLV.ItemsSource = suitableItems;
    }

    private void AddNewItemDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        queryBox.Text = string.Empty;
        errorInfoBar.IsOpen = false;
        errorInfoBar.Message = string.Empty;
    }

    private void AddNewItemDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        bool valid = Validate();
        args.Cancel = !valid;
        errorInfoBar.IsOpen = !valid;
        if (valid)
        {
            Result = EditorResult.Create;
            if (!_newProject) SelectedItemType = (newItemsLV.SelectedItem as NewItemType)?.Type;
            ItemTitle = titleTextBox.Text;
        }
    }

    private void AddNewItemDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = EditorResult.Cancel;
    }

    private bool Validate()
    {
        if (!string.IsNullOrWhiteSpace(titleTextBox.Text))
        {
            if (_newProject) return true;

            if (newItemsLV.SelectedItem is NewItemType itemType)
            {
                if ((ParentResource != null
                    && ParentResource.CanAddChildResourceType(itemType.Type))
                    || ParentResource == null)
                    return true;
                else
                    errorInfoBar.Message = "Cannot add a new item to the parent item (" + ParentResource?.Title + ")";
            }
            else
                errorInfoBar.Message = "An item type must be selected";
        }
        else
            errorInfoBar.Message = "Title must not be empty.";

        return false;
    }

    private bool _newProject = false;

    public async Task<EditorResult> CreateProjectAsync()
    {
        _newProject = true;
        parentInfo.Visibility = Visibility.Collapsed;
        queryBox.Visibility = Visibility.Collapsed;
        newItemsLV.Visibility = Visibility.Collapsed;
        ItemTitle = titleTextBox.Text = string.Empty;
        Title = "Create A New Project";
        PrimaryButtonText = "Create";
        await ShowAsync(ContentDialogPlacement.Popup);
        return Result;
    }

    public async Task<EditorResult> CreateAsync(IResource? parentResource)
    {
        _newProject = false;
        queryBox.Visibility = Visibility.Visible;
        newItemsLV.Visibility = Visibility.Visible;
        ParentResource = parentResource;
        SetParentInfo(parentResource);
        FilterItems(parentResource);
        ItemTitle = titleTextBox.Text = string.Empty;
        Title = "Add New Item";
        PrimaryButtonText = "Add";
        await ShowAsync(ContentDialogPlacement.Popup);
        return Result;
    }

    private void SetParentInfo(IResource? parentResource)
    {
        if (parentResource == null)
        {
            parentInfo.Text = string.Empty;
            parentInfo.Visibility = Visibility.Collapsed;
        }
        else
        {
            parentInfo.Visibility = Visibility.Visible;
            parentInfo.Text = $"The new item will be added to {parentResource.Title}";
        }
    }

    private void FilterItems(IResource? parentResource)
    {
        if (parentResource == null)
        {
            newItemsLV.ItemsSource = NewItemType.KnownTypes;
            availItemTypes = NewItemType.KnownTypes;
            return;
        }

        List<NewItemType> items = NewItemType.KnownTypes.FindAll(x => parentResource.CanHandleChildResourceType(x.Type));
        newItemsLV.ItemsSource = items;
        availItemTypes = items;
    }
}
