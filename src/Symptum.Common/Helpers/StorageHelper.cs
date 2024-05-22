namespace Symptum.Common.Helpers;

public class StorageHelper
{
    private static bool _isFileOpenPickerSupported = true;
    private static bool _isFileSavePickerSupported = true;
    private static bool _isFolderPickerSupported = true;

    public static void SetPickerSupport(bool isFileOpenPickerSupported, bool isFileSavePickerSupported, bool isFolderPickerSupported)
    {
        _isFileOpenPickerSupported = isFileOpenPickerSupported;
        _isFileSavePickerSupported = isFileSavePickerSupported;
        _isFolderPickerSupported = isFolderPickerSupported;
    }

    public static bool IsFileOpenPickerSupported { get => _isFileOpenPickerSupported; }

    public static bool IsFileSavePickerSupported { get => _isFileSavePickerSupported; }

    public static bool IsFolderPickerSupported { get => _isFolderPickerSupported; }
}
