using Symptum.Core.Subjects;
using Symptum.Editor.Helpers;

namespace Symptum.Editor.Controls;
public sealed partial class QuestionBankContextConfigureDialog : ContentDialog
{
    private QuestionBankContext? context;

    public QuestionBankContextConfigureDialog()
    {
        InitializeComponent();

        context ??= QuestionBankContextHelper.CurrentContext;
        scCB.ItemsSource = Enum.GetValues(typeof(SubjectList));

        Opened += QuestionBankContextConfigureDialog_Opened;
        PrimaryButtonClick += QuestionBankContextConfigureDialog_PrimaryButtonClick;
        SecondaryButtonClick += QuestionBankContextConfigureDialog_SecondaryButtonClick;
    }

    private void QuestionBankContextConfigureDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        if (context != null)
        {
            scCB.SelectedItem = context.SubjectCode;
            if (context.LastInputDate is DateOnly dateOnly)
            {
                datePicker.Date = new(dateOnly.ToDateTime(new TimeOnly(0)));
            }
            bookRefPicker.PresetBookReference = context.PreferredBook ?? new();
        }
    }

    private void QuestionBankContextConfigureDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (context != null)
        {
            context.SubjectCode = scCB.SelectedItem != null ? (SubjectList)scCB.SelectedItem : SubjectList.None;
            context.LastInputDate = DateOnly.FromDateTime(datePicker.SelectedDate?.Date ?? DateTime.Now);
            context.PreferredBook = bookRefPicker.PresetBookReference;
        }
    }

    private void QuestionBankContextConfigureDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        scCB.SelectedItem = null;
        datePicker.SelectedDate = null;
        bookRefPicker.PresetBookReference = null;
    }
}
