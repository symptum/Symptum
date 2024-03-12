using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects.QuestionBanks;
using Windows.Storage.Pickers;

namespace Symptum.Editor.Helpers;

public class ResourceHelper
{
    private static StorageFolder? workFolder;

    private static XamlRoot? _xamlRoot;
    private static IntPtr _hWnd = IntPtr.Zero;

    private static bool _folderPicked = false;

    public static bool FolderPicked
    {
        get => _folderPicked;
        set => _folderPicked = value;
    }

    public static void Initialize(XamlRoot? xamlRoot, IntPtr hWnd)
    {
        _xamlRoot = xamlRoot;
        _hWnd = hWnd;
    }

    public static async Task<IReadOnlyList<StorageFile>?> GetFilesFromWorkPathAsync()
    {
        if (workFolder != null)
        {
            return await workFolder.GetFilesAsync();
        }

        return null;
    }

    public static async Task<bool> VerifyWorkPathAsync()
    {
        bool pathExists;
        if (workFolder == null)
            pathExists = await SelectWorkPathAsync();
        else pathExists = true;

        return pathExists;
    }

    public static async Task<bool> SelectWorkPathAsync()
    {
        StorageFolder folder;
        if (StorageHelper.IsFolderPickerSupported)
        {
            FolderPicker folderPicker = new();
            folderPicker.FileTypeFilter.Add("*");

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, _hWnd);
#endif
            folder = await folderPicker.PickSingleFolderAsync();
            _folderPicked = true;
        }
        else
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            folder = await localFolder.CreateFolderAsync("Temp", CreationCollisionOption.OpenIfExists);
            _folderPicked = false;
        }

        if (folder != null && workFolder != folder)
        {
            workFolder = folder;
            return true;
        }

        return false;
    }

    public static async Task<bool> SaveResourceAsync(IResource resource)
    {
        if (resource is QuestionBankTopic topic)
        {
            return await SaveQuestionBankTopicAsync(topic);
        }

        return false;
    }

    public static async Task<bool> SaveQuestionBankTopicAsync(QuestionBankTopic topic)
    {
        if (topic == null) return false;

        if (_folderPicked && workFolder != null)
        {
            var file = await workFolder.CreateFileAsync(topic.Title + ".csv", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, topic.ToCSV());
            return true;
        }
        else
        {
            var fileSavePicker = new FileSavePicker
            {
                SuggestedFileName = topic.Title
            };
            fileSavePicker.FileTypeChoices.Add("CSV File", [".csv"]);

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
            WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, _hWnd);
#endif
            StorageFile saveFile = await fileSavePicker.PickSaveFileAsync();
            if (saveFile != null)
            {
                CachedFileManager.DeferUpdates(saveFile);
                await FileIO.WriteTextAsync(saveFile, topic.ToCSV());
                await CachedFileManager.CompleteUpdatesAsync(saveFile);
                return true;
            }
        }

        return false;
    }
}
