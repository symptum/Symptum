using Microsoft.UI.Xaml.Markup;
using Symptum.Editor.ViewModels;

namespace Symptum.Editor.Common;

[MarkupExtensionReturnType(ReturnType = typeof(ICommand))]
internal class GetRelayCommandExtension : MarkupExtension
{
    public string? Name { get; set; }

    protected override object? ProvideValue()
    {
        return Name switch
        {
            nameof(MainViewModel.AddNewItemCommand) => MainViewModel.Instance.AddNewItemCommand,
            nameof(MainViewModel.DeleteResourceCommand) => MainViewModel.Instance.DeleteResourceCommand,
            _ => null
        };
    }
}
