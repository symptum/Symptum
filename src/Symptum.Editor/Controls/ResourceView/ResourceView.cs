using System.Collections.ObjectModel;

namespace Symptum.Editor.Controls;

public sealed partial class ResourceView : Control
{
    private ScrollViewer? _scrollViewer;
    private ItemsRepeater? _itemsRepeater;

    internal ObservableCollection<ResourceViewNode> FlattenedItems { get; } = new();

    public ResourceView()
    {
        DefaultStyleKey = typeof(ResourceView);
    }

    protected override void OnApplyTemplate()
    {
        if (_itemsRepeater != null)
        {
            _itemsRepeater.ElementPrepared -= OnElementPrepared;
            _itemsRepeater.ElementClearing -= OnElementClearing;
            _itemsRepeater.ItemsSource = null;
        }

        _scrollViewer = GetTemplateChild("scrollViewer") as ScrollViewer;
        _itemsRepeater = GetTemplateChild("itemsRepeater") as ItemsRepeater;

        if (_itemsRepeater != null)
        {
            _itemsRepeater.ItemsSource = FlattenedItems;
            _itemsRepeater.ElementPrepared += OnElementPrepared;
            _itemsRepeater.ElementClearing += OnElementClearing;
        }

        IsTabStop = true;
        RemoveHandler(KeyDownEvent, new KeyEventHandler(OnResourceViewKeyDown));
        AddHandler(KeyDownEvent, new KeyEventHandler(OnResourceViewKeyDown), true);
    }
}
