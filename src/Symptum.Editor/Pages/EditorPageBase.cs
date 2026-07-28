using System.ComponentModel;
using Symptum.Core.Management.Resources;
using Symptum.Editor.ViewModels;

namespace Symptum.Editor.Pages;

public partial class EditorPageBase : Page, IEditorPage
{
    #region Properties

    public string? PageName { get; protected set; }

    public IconSource? IconSource { get; protected set; }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(EditorPageBase),
            new PropertyMetadata(null));

    public string? Title
    {
        get => GetValue(TitleProperty) as string;
        set => SetValue(TitleProperty, value);
    }

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
            if (e.OldValue is INotifyPropertyChanged old)
                old.PropertyChanged -= editorPageBase.HandlePropertyChanged;
            if (e.NewValue is INotifyPropertyChanged @new)
            {
                var resource = @new as IResource;
                editorPageBase.OnSetEditableContent(resource);
                editorPageBase.Title = resource?.Title;
                @new.PropertyChanged += editorPageBase.HandlePropertyChanged;
            }
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

    private void HandlePropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Title")
        {
            Title = (s as IResource)?.Title;
        }
    }

    protected virtual void OnSetEditableContent(IResource? resource) { }

    public void UpdateContent() => OnUpdateContent();

    protected virtual void OnUpdateContent() { }

    protected virtual void OnCleanupPage() { }

    public new void Dispose()
    {
        OnCleanupPage();
    }

    protected void WriteToOutput(string message)
    {
        MainViewModel.AddOutputEntry(message, PageName);
    }
}
