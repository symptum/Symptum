using Symptum.Common.Helpers;
using Symptum.Core.Management.Resources;
using Symptum.UI;

namespace Symptum.Editor.Pages;

public sealed partial class AudioEditorPage : EditorPageBase
{
    public AudioEditorPage()
    {
        InitializeComponent();
        PageName = "Audio Visualizer";
        IconSource = IconHelper.AudioIconSource;
        Loaded += AudioEditorPage_Loaded;
    }

    private async void AudioEditorPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (EditableContent is not AudioFileResource audioFileResource) return;
        if (await ResourceHelper.GetStorageFileAsync(audioFileResource) is StorageFile file)
        {
            audioVisualizer.Source = file;
        }

        WriteToOutput($"Loaded audio: {audioFileResource.Title}");
    }

    protected override void OnCleanupPage()
    {
        audioVisualizer.Unload();
    }
}
