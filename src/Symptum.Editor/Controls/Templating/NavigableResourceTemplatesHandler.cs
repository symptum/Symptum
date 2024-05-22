using System.Collections.ObjectModel;

namespace Symptum.Editor.Controls.Templating;

public class NavigableResourceTemplatesHandler
{
    public static NavigableResourceTemplatesHandler Current { get; set; } = new();

    public NavigableResourceTemplatesHandler()
    {
        Templates = [];
        AddDefaultTemplates();
    }

    public ObservableCollection<TypedDataTemplate> Templates { get; set; }

    private void AddDefaultTemplates()
    {
        var templates = new TreeViewNavigableResourceTemplates();
        foreach (var i in templates)
        {
            if (i.Value is TypedDataTemplate template)
                Templates.Add(template);
        }
    }
}
