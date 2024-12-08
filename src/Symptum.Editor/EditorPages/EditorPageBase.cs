using Symptum.Core.Management.Resources;

namespace Symptum.Editor.EditorPages;

public partial class EditorPageBase : Page, IEditorPage
{
    #region Properties

    public IconSource? IconSource { get; protected set; }

    public static readonly DependencyProperty EditableContentProperty =
        DependencyProperty.Register(
            nameof(EditableContent),
            typeof(IResource),
            typeof(EditorPageBase),
            new PropertyMetadata(null, OnEditableContentChanged));

    private static void OnEditableContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EditorPageBase editorPageBase)
        {
            editorPageBase.OnSetEditableContent(e.NewValue as IResource);
        }
    }

    public IResource? EditableContent
    {
        get => (IResource?)GetValue(EditableContentProperty);
        set => SetValue(EditableContentProperty, value);
    }

    public static readonly DependencyProperty HasUnsavedChangesProperty = DependencyProperty.Register(
        nameof(HasUnsavedChanges),
        typeof(bool),
        typeof(EditorPageBase),
        new PropertyMetadata(false));

    public bool HasUnsavedChanges
    {
        get => (bool)GetValue(HasUnsavedChangesProperty);
        set => SetValue(HasUnsavedChangesProperty, value);
    }

    #endregion

    protected virtual void OnSetEditableContent(IResource? resource) { }
}
