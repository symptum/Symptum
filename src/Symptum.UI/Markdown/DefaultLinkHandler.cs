using Symptum.Core.Management.Resources;
using Windows.System;

namespace Symptum.UI.Markdown;

public sealed class DefaultLinkHandler : ILinkHandler
{
    private DocumentOutline _documentOutline;

    public DefaultLinkHandler(DocumentOutline documentOutline)
    {
        _documentOutline = documentOutline;
    }

    public async void HandleNavigation(string? url, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        if (url.StartsWith('#'))
        {
            if (_documentOutline.IdNavigateCollection.TryGetValue(url[1..], out Action? navigate))
                navigate!();
        }
        else if (url.StartsWith(ResourceManager.DefaultUriScheme))
        {
            NavigationRequested?.Invoke(null, new Uri(url));
        }
        else
        {
            Uri uri = Helper.GetUri(url, baseUrl);
            await Launcher.LaunchUriAsync(uri);
        }
    }

    public event EventHandler<Uri> NavigationRequested;
}
