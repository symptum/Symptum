namespace Symptum.Core.Helpers;

public static class FileHelper
{
    public const char PathSeparator = '\\';

    public const char ExtensionSeparator = '.';

    public const string JsonFileExtension = ".json";

    public const string CsvFileExtension = ".csv";

    public const string PackageFileExtension = ".zip";

    public static (string folder, string fileName, string extension) GetDetailsFromFilePath(string? filePath)
    {
        string folder = string.Empty;
        string fileName = string.Empty;
        string extension = string.Empty;

        if (filePath == null) return (folder, fileName, extension);

        int dotIndex, slashIndex;
        dotIndex = slashIndex = filePath.Length;

        for (int i = filePath.Length - 1; i >= 0; i--)
        {
            char ch = filePath[i];
            if (ch == PathSeparator)
            {
                slashIndex = i + 1; // To include '\'
                break;
            }
            else if (ch == ExtensionSeparator)
            {
                dotIndex = i;
                continue;
            }
        }

        if (dotIndex > 0 && slashIndex > 0)
        {
            folder = filePath[..slashIndex];
            fileName = filePath[slashIndex..dotIndex];
            extension = filePath[dotIndex..];
        }

        return (folder, fileName, extension);
    }
}
