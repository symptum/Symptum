using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using Symptum.Core.Subjects.QuestionBanks;

namespace Symptum.Editor.Controls;

public sealed partial class AddNewItemDialog : ContentDialog
{
    public static readonly List<NewItemType> itemTypes =
        [
            new ("Subject", typeof(Subject), "Subjects"),
            new("Question Bank", typeof(QuestionBank), "Question Banks"),
            new("Question Bank Paper", typeof(QuestionBankPaper), "Question Banks"),
            new("Question Bank Topic", typeof(QuestionBankTopic), "Question Banks"),
            new("Reference Values Package", typeof(ReferenceValuesPackage), "Reference Values"),
            new("Reference Value Family", typeof(ReferenceValueFamily), "Reference Values"),
            new("Reference Value Group", typeof(ReferenceValueGroup), "Reference Values")
        ];

    private List<NewItemType> availItemTypes;

    #region Properties

    public IResource? ParentResource { get; set; } = null;

    public string ItemTitle { get; set; } = string.Empty;

    public Type? SelectedItemType { get; set; } = null;

    public EditorResult Result { get; private set; } = EditorResult.None;

    #endregion

    public AddNewItemDialog()
    {
        InitializeComponent();
        newItemsLV.ItemsSource = itemTypes;
        Opened += AddNewItemDialog_Opened;
        PrimaryButtonClick += AddNewItemDialog_PrimaryButtonClick;
        CloseButtonClick += AddNewItemDialog_CloseButtonClick;
    }

    private void AddNewItemDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        errorInfoBar.IsOpen = false;
        errorInfoBar.Message = string.Empty;
    }

    private void AddNewItemDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        bool valid = Validate();
        args.Cancel = !valid;

        if (valid)
        {
            Result = EditorResult.Create;
            SelectedItemType = (newItemsLV.SelectedItem as NewItemType)?.Type;
            ItemTitle = titleTextBox.Text;
        }
    }

    private void AddNewItemDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = EditorResult.Cancel;
    }

    private bool Validate()
    {
        bool valid = false;

        if (newItemsLV.SelectedItem is NewItemType itemType)
        {
            valid = (ParentResource != null
                && ParentResource.CanAddChildResourceType(itemType.Type))
                || ParentResource == null;

            if (valid)
            {
                var title = titleTextBox.Text;
                valid = !string.IsNullOrEmpty(title)
                    && !string.IsNullOrWhiteSpace(title);

                if (!valid)
                    errorInfoBar.Message = "Title must not be empty";
            }
            else
                errorInfoBar.Message = "Cannot add a new item to the parent item (" + ParentResource?.Title + ")";
        }
        else
            errorInfoBar.Message = "An item type must be selected";

        errorInfoBar.IsOpen = !valid;
        return valid;
    }

    public async Task<EditorResult> CreateAsync(IResource? parentResource)
    {
        ParentResource = parentResource;
        FilterItems(parentResource);
        ItemTitle = titleTextBox.Text = string.Empty;
        await ShowAsync(ContentDialogPlacement.Popup);
        return Result;
    }

    private void FilterItems(IResource? parentResource)
    {
        if (parentResource == null)
        {
            newItemsLV.ItemsSource = itemTypes;
            availItemTypes = itemTypes;
            return;
        }

        List<NewItemType> items = itemTypes.FindAll(x => parentResource.CanHandleChildResourceType(x.Type));
        newItemsLV.ItemsSource = items;
        availItemTypes = items;
    }
}

public class NewItemType
{
    public string DisplayName { get; set; }

    public Type Type { get; set; }

    public string? GroupName { get; set; }

    public NewItemType()
    { }

    public NewItemType(string displayName, Type type, string? groupName = null)
    {
        DisplayName = displayName;
        Type = type;
        GroupName = groupName;
    }
}
