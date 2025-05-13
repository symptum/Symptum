using Symptum.Core.Management.Resources;
using Symptum.Editor.Pages;

namespace Symptum.Editor.Commands;

internal class NavigateResourcesCommand : IEditorCommand
{
    public string Key => string.Empty;

    public Type? EditorPageType { get; } = null;

    public void Execute(EditorCommandOption? option = null)
    {
        if (option?.Parameter is IResource resource)
        {
            EditorPagesManager.CreateOrOpenEditor(resource);
        }
    }

    public IEnumerable<EditorCommandOption> GetOptions(string? parameter = null)
    {
        IEnumerable<IResource> matches = ResourceManager.Resources.Take(10);
        if (!string.IsNullOrWhiteSpace(parameter))
        {
            matches = ResourceManager.Resources.Where(x => x.Title?.Contains(parameter,
                StringComparison.InvariantCultureIgnoreCase) ?? false).Take(10);
        }

        IEnumerable<EditorCommandOption> options = matches.Select(resource => new EditorCommandOption(resource.Title, resource));
        return options;
    }
}
