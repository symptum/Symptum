using Symptum.Core.Management.Resources;

namespace Symptum.Editor.Controls.Templating;

public class NavigableResourceTemplateSelector : DataTemplateSelector
{
    public DataTemplate DefaultDataTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is NavigableResource resource)
        {
            var template = NavigableResourceTemplatesHandler.Instance?.Templates.FirstOrDefault(x => x.DataType == item.GetType())?.DataTemplate;
            return template ?? DefaultDataTemplate;
        }
        return base.SelectTemplateCore(item);
    }
}
