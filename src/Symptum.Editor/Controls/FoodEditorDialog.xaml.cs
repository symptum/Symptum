using System.Collections.ObjectModel;
using Symptum.Core.Data.Nutrition;

namespace Symptum.Editor.Controls;

public sealed partial class FoodEditorDialog : ContentDialog
{
    public Food? Food { get; private set; }

    public EditorResult EditResult { get; private set; } = EditorResult.None;

    private readonly ObservableCollection<ListEditorItemWrapper<FoodMeasure>> measures = [];

    public FoodEditorDialog()
    {
        InitializeComponent();
        AddControls();
        Opened += FoodEditor_Opened;
        PrimaryButtonClick += FoodEditor_PrimaryButtonClick;
        CloseButtonClick += FoodEditor_CloseButtonClick;

        HandleListEditors();
    }

    private void FoodEditor_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = EditorResult.Cancel;
        ClearFood();
    }

    private void FoodEditor_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = _isCreate ? EditorResult.Create : EditorResult.Update;
        UpdateFood();
        ClearFood();
    }

    private void FoodEditor_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        LoadFood();
    }

    private bool _isCreate = false;

    public async Task<EditorResult> CreateAsync()
    {
        Title = "Add a New Food";
        PrimaryButtonText = "Add";
        Food = null;
        _isCreate = true;
        await ShowAsync();
        return EditResult;
    }

    public async Task<EditorResult> EditAsync(Food food)
    {
        Title = "Edit Food";
        PrimaryButtonText = "Update";
        Food = food;
        _isCreate = false;
        await ShowAsync();
        return EditResult;
    }

    private void LoadFood()
    {
        if (Food == null) return;

        idTB.Text = Food.Id;
        titleTB.Text = Food.Title;
        measures.LoadFromList(Food.Measures);
        LoadNutrients();
    }

    private void UpdateFood()
    {
        Food ??= new();
        Food.Id = idTB.Text;
        Food.Title = titleTB.Text;
        Food.Measures = measures.UnwrapToList();
        UpdateNutrients();
    }

    private void ClearFood()
    {
        idTB.Text = string.Empty;
        titleTB.Text = string.Empty;
        measures.Clear();
        ClearNutrients();
    }

    private void HandleListEditors()
    {
        meLE.ItemsSource = measures;
        meLE.AddItemRequested += (s, e) => measures.Add(new ListEditorItemWrapper<FoodMeasure>(new()));
        meLE.ClearItemsRequested += (s, e) => measures.Clear();
        meLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<FoodMeasure> measure)
                measures.Remove(measure);
        };
        meLE.DuplicateItemRequested += (s, e) =>
        {
            //if (e is ListEditorItemWrapper<FoodMeasure> measure)
            //    measures.Add(new() { Value = measure.Value });
        };
        meLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<FoodMeasure> measure)
            {
                int oldIndex = measures.IndexOf(measure);
                int newIndex = Math.Max(oldIndex - 1, 0);
                measures.Move(oldIndex, newIndex);
            }
        };
        meLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<FoodMeasure> measure)
            {
                int oldIndex = measures.IndexOf(measure);
                int newIndex = Math.Min(oldIndex + 1, measures.Count - 1);
                measures.Move(oldIndex, newIndex);
            }
        };
    }
}
