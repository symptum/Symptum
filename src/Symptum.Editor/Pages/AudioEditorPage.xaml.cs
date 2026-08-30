using Symptum.Common.Helpers;
using Symptum.Common.ProjectSystem;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Controls;
using Symptum.UI;

namespace Symptum.Editor.Pages;

public sealed partial class AudioEditorPage : EditorPageBase
{
    private AudioFileResource? _audioFileResource;
    private ResourcePropertiesEditorDialog? propertyEditorDialog;

    public AudioEditorPage()
    {
        InitializeComponent();
        PageName = "Audio Visualizer";
        IconSource = IconHelper.AudioIconSource;
        Loaded += AudioEditorPage_Loaded;
    }

    private async void AudioEditorPage_Loaded(object sender, RoutedEventArgs e)
    {
        propertyEditorDialog = EditorPagesManager.CreateOrGetDialog<ResourcePropertiesEditorDialog>();
        if (EditableContent is not AudioFileResource audioFileResource) return;

        _audioFileResource = audioFileResource;

        if (await ResourceHelper.GetStorageFileAsync(audioFileResource) is StorageFile file)
        {
            audioVisualizer.Source = file;
        }

        WriteToOutput($"Loaded audio: {audioFileResource.Title}");
    }

    private bool _isBeingSaved = false;

    private async void AudioVisualizer_ActionButtonClicked(object sender, EventArgs e)
    {
        if (_audioFileResource != null && propertyEditorDialog != null)
        {
            propertyEditorDialog.XamlRoot = XamlRoot;
            var result = await propertyEditorDialog.EditAsync(_audioFileResource);
            if (result == EditorResult.Update)
            {
                if (_isBeingSaved) return;

                _isBeingSaved = true;
                HasUnsavedChanges = !await ProjectSystemManager.SaveResourceAndAncestorAsync(_audioFileResource);
                _isBeingSaved = false;

                WriteToOutput($"Updated properties and saved: {_audioFileResource.Title}");
            }
        }
    }

    protected override void OnCleanupPage()
    {
        audioVisualizer.Unload();
    }

}
