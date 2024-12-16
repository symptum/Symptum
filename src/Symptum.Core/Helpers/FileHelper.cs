using System.Text;

namespace Symptum.Core.Helpers;

public static class FileHelper
{
    public const char PathSeparator = '\\';

    public const char ExtensionSeparator = '.';

    public const string ProjectFileExtension = ".symproj";

    public const string JsonFileExtension = ".json";

    public const string CsvFileExtension = ".csv";

    public const string MarkdownFileExtension = ".md";

    public const string PackageFileExtension = ".zip";

    #region Image File Extensions

    public static readonly string[] ImageFileExtensions =
    [
        JpegFileExtension,
        JpgFileExtension,
        PngFileExtension,
        BmpFileExtension,
        SvgFileExtension
    ];

    public const string JpegFileExtension = ".jpeg";

    public const string JpgFileExtension = ".jpg";

    public const string PngFileExtension = ".png";

    public const string BmpFileExtension = ".bmp";

    public const string SvgFileExtension = ".svg";

    #endregion

    #region Audio File Extensions

    public static readonly string[] AudioFileExtensions =
    [
        Mp3FileExtension
    ];

    public const string Mp3FileExtension = ".mp3";

    #endregion

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

        if (slashIndex > 0)
            folder = filePath[..slashIndex];

        if (dotIndex > 0)
        {
            if (slashIndex > 0)
                fileName = filePath[slashIndex..dotIndex];
            else
                fileName = filePath[..dotIndex];
            extension = filePath[dotIndex..];
        }

        return (folder, fileName, extension);
    }

    /// <summary>
    /// Removes illegal characters from the given text that are not allowed in file names.
    /// </summary>
    /// <param name="text">The input text from which illegal characters need to be removed.</param>
    /// <param name="predicate">An optional function to test each character for a condition.</param>
    /// <returns>A new string with all illegal characters removed. If the input text is null or empty, an empty string is returned.</returns>
    public static string RemoveIllegalCharacters(string? text, Func<char, bool> predicate = null)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        char[] invalidChars = Path.GetInvalidFileNameChars();
        predicate ??= (ch) => true;
        return new([.. text.Where(ch => predicate(ch) && !invalidChars.Contains(ch))]);
    }

    /// <summary>
    /// Converts the given bytes into a human readable format (i.e. KB, MB, GB, etc.)
    /// </summary>
    /// <param name="bytes">The total bytes to be formatted.</param>
    /// <param name="addSuffix">Specifies whether to add <strong>unit</strong> suffix (eg. 20 <strong>MB</strong>).</param>
    /// <returns>The formatted value of the given bytes.</returns>
    public static string FormatSize(ulong bytes, bool addSuffix = true)
    {
        string[] suf = { " Bytes", " KB", " MB", " GB", " TB", " PB", " EB" };

        if (bytes == 0L) { return "0" + (addSuffix ? suf[0] : string.Empty); }

        int place = (int)System.Math.Floor(System.Math.Log(bytes, 1024));
        double num = System.Math.Round(bytes / System.Math.Pow(1024, place), 2);
        return num.ToString() + (addSuffix ? suf[place] : string.Empty);
    }
}
