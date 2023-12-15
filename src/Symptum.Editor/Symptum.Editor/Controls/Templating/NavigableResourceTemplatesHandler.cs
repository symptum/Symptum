using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symptum.Editor.Controls.Templating;

public class NavigableResourceTemplatesHandler
{
    public static NavigableResourceTemplatesHandler Instance { get; set; } = new();

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
