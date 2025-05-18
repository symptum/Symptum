using Symptum.Core.Management.Resources;
using Symptum.Editor.Pages;

namespace Symptum.Editor.Commands;

internal class NavigateResourcesCommand : IEditorCommand
{
    public string Key => string.Empty;

    public Type? EditorPageType { get; } = null;

    public int NumberOfArguments => 1;

    public void Execute(IEnumerable<EditorCommandArgument>? args = null)
    {
        if (args?.FirstOrDefault()?.Parameter is IResource resource)
        {
            EditorPagesManager.CreateOrOpenEditor(resource);
        }
    }

    public async Task<EditorCommandArgumentRequest?> GetRequestAsync(string? text = null, int argIndex = 0)
    {
        return await Task.Run<EditorCommandArgumentRequest>(() =>
        {
            IEnumerable<IResource> matches = ResourceManager.Resources.Take(10);
            if (!string.IsNullOrWhiteSpace(text))
            {
                matches = ResourceManager.Resources.Where(x => x.Title?.Contains(text,
                    StringComparison.InvariantCultureIgnoreCase) ?? false).Take(10);
            }

            IEnumerable<EditorCommandArgument> args = matches.Select(resource => new EditorCommandArgument(resource.Title, resource));
            return new("Select a resource", EditorCommandArgumentRequestType.Search, args);
        });
    }
}
