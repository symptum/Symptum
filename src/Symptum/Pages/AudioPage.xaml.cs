using Symptum.Common.Helpers;
using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;

namespace Symptum.Pages;

public sealed partial class AudioPage : NavigablePage
{
    private AudioFileResource? _audioResource;

    public AudioPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected override void OnNavigableChanged(INavigable? navigable)
    {
        if (navigable is AudioFileResource audioResource)
        {
            _audioResource = audioResource;
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_audioResource == null) return;

        if (await ResourceHelper.GetStorageFileAsync(_audioResource) is StorageFile file)
        {
            audioVisualizer.Source = file;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _audioResource = null;
        audioVisualizer.Unload();
    }

    protected override void OnLayoutChanged(bool isWide) => audioVisualizer.Padding = isWide switch
    {
        true => ContentMargin,
        false => NarrowContentMargin
    };
}
