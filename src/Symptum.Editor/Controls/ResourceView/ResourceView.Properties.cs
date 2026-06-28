using Symptum.Core.Management.Resources;

namespace Symptum.Editor.Controls;

public sealed partial class ResourceView
{
    public IReadOnlyList<IResource>? Source
    {
        get => (IReadOnlyList<IResource>?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(IReadOnlyList<IResource>),
        typeof(ResourceView),
        new PropertyMetadata(null, OnSourceChanged));

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResourceView view && e.NewValue is IReadOnlyList<IResource> resources)
        {
            view.SetRootNodes(resources);
        }
    }

    public IResource? SelectedResource
    {
        get => (IResource?)GetValue(SelectedResourceProperty);
        set => SetValue(SelectedResourceProperty, value);
    }

    public static readonly DependencyProperty SelectedResourceProperty = DependencyProperty.Register(
        nameof(SelectedResource),
        typeof(IResource),
        typeof(ResourceView),
        new PropertyMetadata(null));

    public IList<IResource> SelectedResources
    {
        get => (IList<IResource>)GetValue(SelectedResourcesProperty);
        set => SetValue(SelectedResourcesProperty, value);
    }

    public static readonly DependencyProperty SelectedResourcesProperty = DependencyProperty.Register(
        nameof(SelectedResources),
        typeof(IList<IResource>),
        typeof(ResourceView),
        new PropertyMetadata(null));

    public ResourceViewSelectionMode SelectionMode
    {
        get => (ResourceViewSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(
        nameof(SelectionMode),
        typeof(ResourceViewSelectionMode),
        typeof(ResourceView),
        new PropertyMetadata(ResourceViewSelectionMode.Single, OnSelectionModeChanged));

    private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ResourceView view)
        {
            var newMode = (ResourceViewSelectionMode)e.NewValue;
            if (newMode != ResourceViewSelectionMode.Multiple)
                view.ClearSelection();
            view.UpdateAllItemVisuals();
        }
    }

    public event EventHandler<IResource>? ResourceOpenRequested;

    public event EventHandler<IList<IResource>>? ResourcesSelected;
}
