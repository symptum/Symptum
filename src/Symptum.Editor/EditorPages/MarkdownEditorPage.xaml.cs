using System.Text;
using Symptum.Common.Helpers;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Common;
using Symptum.Editor.Controls;
using Symptum.UI.Markdown;

namespace Symptum.Editor.EditorPages;

public sealed partial class MarkdownEditorPage : EditorPageBase
{
    private MarkdownFileResource? _markdownResource;
    private ResourcePropertiesEditorDialog propertyEditorDialog = new();
    private const string m_CurrentDocument = "Current Document";
    private const string m_Selection = "Selection";

    public MarkdownEditorPage()
    {
        InitializeComponent();
        mdText.Text = string.Join("\r\n", quotesMD, tabledMd); // Temp
        IconSource = DefaultIconSources.DocumentIconSource;
        mdText.TextChanged += (s, e) =>
        {
            _mdDirtyForSearch = true;
            HasUnsavedChanges = true;
            UpdateStatusBar(onlyCount: true);
        };
        mdText.SelectionChanged += (s, e) => UpdateStatusBar(onlyCaret: true, onlyCount: true);
        mdText.KeyDown += MdText_KeyDown;
        mdText.KeyUp += MdText_KeyUp;

        SetupFindControl();
    }

    private void UpdateStatusBar(bool onlyCaret = false, bool onlyCount = false)
    {
        if (onlyCaret)
        {
            (int line, int col) = mdText.Text.GetLineAndColumnIndex(mdText.SelectionStart + mdText.SelectionLength);
            caretTB.Text = $"Ln {line}, Col {col}";
        }

        if (onlyCount)
        {
            int slen = mdText.SelectionLength;
            countTB.Text = (slen > 0 ? slen.ToString() + " of " : string.Empty) + mdText.Text.Length.ToString() + " characters";
        }
    }

    #region Ctrl + I hack

    private void MdText_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Control) ctrlPressed = false;
        else if (e.Key == Windows.System.VirtualKey.Shift) shiftPressed = false;
    }

    private bool ctrlPressed = false;
    private bool shiftPressed = false;

    private void MdText_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Control) ctrlPressed = true;
        else if (e.Key == Windows.System.VirtualKey.Shift) shiftPressed = true;

        if (e.Key == Windows.System.VirtualKey.I && ctrlPressed && !shiftPressed)
        {
            e.Handled = true;
            ToggleWrap('*');
        }
    }

    #endregion

    protected override void OnSetEditableContent(IResource? resource)
    {
        if (resource is MarkdownFileResource markdownResource)
        {
            _markdownResource = markdownResource;
            mdText.Text = markdownResource.Markdown;
        }
    }

    #region Temp

    private string quotesMD = @"# Quotes

> Text that is a quote

## Alerts

> [!NOTE]
> Useful information that users should know, even when skimming content.

> [!TIP]
> Helpful advice for doing things better or more easily.

> [!IMPORTANT]
> Key information users need to know to achieve their goal.

> [!WARNING]
> Urgent info that needs immediate user attention to avoid problems.

> [!CAUTION]
> Advises about risks or negative outcomes of certain actions.
";
    private string tabledMd = @"# Tables

| abc | def | ghi |
|:---:|-----|----:|
|  1  | 2   | 3   |

+---------+---------+
| Header  | Header  |
| Column1 | Column2 |
+=========+=========+
| 1. ab   | > This is a quote
| 2. cde  | > For the second column 
| 3. f    |
+---------+---------+
| Second row spanning
| on two columns
+---------+---------+
| Back    |         |
| to      |         |
| one     |         |
| column  |         | 

+---------+---------+
| This is | a table |
+=========+=========+

+---+---+---+
| AAAAA | B |
+ AAAAA +---+
| AAAAA | C |
+---+---+---+
| D | E | F |
+---+---+---+

+---+---+---+
| AAAAA | B |
+---+---+ B +
| D | E | B |
+ D +---+---+
| D | CCCCC |
+---+---+---+
";

    #endregion

    private void TreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is DocumentNode node && node.Navigate is Action navigate)
        {
            navigate();
        }
    }

    private bool _isBeingSaved = false;

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved) return;

        _isBeingSaved = true;

        if (_markdownResource != null)
        {
            _markdownResource.Markdown = mdText.Text;
            HasUnsavedChanges = !await ResourceHelper.SaveResourceAsync(_markdownResource);
        }

        _isBeingSaved = false;
    }

    private async void PropsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_markdownResource != null)
        {
            propertyEditorDialog.XamlRoot = XamlRoot;
            var result = await propertyEditorDialog.EditAsync(_markdownResource);
            if (result == EditorResult.Update)
                HasUnsavedChanges = true;
        }
    }

    #region Find

    private void SetupFindControl()
    {
        List<string> columns =
        [
            m_CurrentDocument,
            m_Selection
        ];

        findControl.FindContexts = columns;
        findControl.SelectedContext = columns[0];
    }

    private void FindButton_Click(object sender, RoutedEventArgs e)
    {
        findControl.Visibility = Visibility.Visible;
    }

    private void FindControl_QueryCleared(object? sender, EventArgs e)
    {
        _searchText = null;
        searchIndices = [];
        currentSearchIndex = 0;
        findControl.Visibility = Visibility.Collapsed;
    }

    private bool _mdDirtyForSearch = false;
    private bool _matchCase = false;
    private bool _matchWholeWord = false;
    private string? _currentContext;
    private string? _searchText;
    private int[] searchIndices;
    private int currentSearchIndex = 0;

    private bool SearchText(string? searchText, string context, bool matchCase = false, bool matchWholeWord = false)
    {
        // Only search when the search configuration (i.e. query text or markdown text or matchCase or matchWholeWord) is changed.
        if (string.IsNullOrWhiteSpace(searchText) ||
            (searchText.Equals(_searchText, matchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase) && !_mdDirtyForSearch &&
            matchCase == _matchCase && matchWholeWord == _matchWholeWord && _currentContext == context)) return false;

        currentSearchIndex = 0;
        _searchText = searchText;
        _mdDirtyForSearch = false;
        _matchCase = matchCase;
        _matchWholeWord = matchWholeWord;
        _currentContext = context;

        string text = mdText.Text;
        int start = 0;
        int end = text.Length;
        if (context == m_Selection)
        {
            start = mdText.SelectionStart;
            end = start + mdText.SelectionLength;
        }
        searchIndices = text.SearchTextAndFindAllMatches(searchText, start, end, matchCase, matchWholeWord);

        return true;
    }

    private void FindControl_QuerySubmitted(object? sender, FindControlQuerySubmittedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.QueryText)) return;

        bool isNew = SearchText(e.QueryText, e.Context, e.MatchCase, e.MatchWholeWord);

        if (searchIndices.Length == 0)
        {
            findControl.ShowErrorMessage(FindError.NoMatches);
        }
        else if (searchIndices.Length == 1 && !isNew)
        {
            findControl.ShowErrorMessage(FindError.NoMoreMatches);
        }
        else
        {
            if (!isNew)
                if (e.FindDirection == FindDirection.Previous)
                    currentSearchIndex--;
                else currentSearchIndex++;

            if (currentSearchIndex >= searchIndices.Length)
                currentSearchIndex = 0;
            else if (currentSearchIndex < 0)
                currentSearchIndex = searchIndices.Length - 1;

            mdText.Select(searchIndices[currentSearchIndex], e.QueryText.Length);
            mdText.Focus(FocusState.Programmatic);
        }
    }

    #endregion

    #region Formatting

    #region Insertion

    private void ToggleLineStarts(string decor, bool onlyInsert = false, bool shouldInsert = true)
    {
        int len = mdText.SelectionLength;
        if (len == 0)
        {
            ToggleLineStart(decor, onlyInsert, shouldInsert);
            return;
        }

        string text = mdText.Text;
        int start = mdText.SelectionStart;
        int end = start + len;

        if (string.IsNullOrWhiteSpace(text) || start < 0 || end > text.Length) return;

        var decorSpan = decor.AsSpan();
        int decorLen = decorSpan.Length;

        ReadOnlySpan<char> span = text.AsSpan();

        StringBuilder result = new();
        int currentPos = 0;
        int addedLen = 0;

        while (currentPos < end)
        {
            int lineStart = currentPos;
            int lineEnd = span[currentPos..].IndexOfAny('\r', '\n');
            lineEnd = lineEnd >= 0 ? currentPos + lineEnd : span.Length;
            var currentLine = span[lineStart..lineEnd];
            if (currentPos < start)
            {
                result.Append(currentLine).AppendLine();
            }
            else // Within selection
            {
                if (onlyInsert || !currentLine.StartsWith(decorSpan))
                {
                    if (shouldInsert)
                    {
                        // Inserting the decor in front of the line.
                        result.Append(decorSpan);
                        addedLen += decorLen;
                    }
                    result.Append(currentLine).AppendLine();
                }
                else
                {
                    // Removing the decor from the line start.
                    result.Append(currentLine[decorLen..]).AppendLine();
                    addedLen -= decorLen;
                }
            }

            currentPos = lineEnd + 1;

            // Consideration for CRLF i.e. \r\n
            if (currentPos < span.Length && span[currentPos - 1] == '\r' && span[currentPos] == '\n')
                currentPos++;
        }

        if (currentPos < span.Length)
            result.Append(span[currentPos..]);

        mdText.Text = result.ToString();
        mdText.Select(start, len + addedLen);
    }

    private void InsertInFrontOfLines(string decor) => ToggleLineStarts(decor, true);

    private void RemoveFromFrontOfLines(string decor) => ToggleLineStarts(decor, false, false);

    private void ToggleLineStart(string decor, bool onlyInsert = false, bool shouldInsert = true)
    {
        int start = mdText.SelectionStart;
        ReadOnlySpan<char> span = mdText.Text.AsSpan();

        if (start < 0 || span.Length < start) return;

        int lineStart = -1;
        for (int i = start - 1; i > 0; i--)
        {
            if (span[i] == '\n' || span[i] == '\r' || span[i] == '\0')
            {
                lineStart = i + 1;
                break;
            }
        }
        if (lineStart < 0) lineStart = 0;

        var before = span[..lineStart];
        var after = span[lineStart..];
        var decorSpan = decor.AsSpan();
        int decorLen = decorSpan.Length;
        StringBuilder result = new();
        if (onlyInsert || !after.StartsWith(decorSpan))
        {
            if (shouldInsert)
            {
                // Inserting the decor in front of the line.
                result.Append(before).Append(decorSpan);
                start += decor.Length;
            }
            result.Append(after);
        }
        else
        {
            // Removing the decor from the line start.
            result.Append(before).Append(after[decorLen..]);
            start -= decor.Length;
        }

        mdText.Text = result.ToString();
        mdText.Select(start, 0);
    }

    private void InsertInFrontOfLine(string decor) => ToggleLineStart(decor, true);

    private void RemoveFromFrontOfLine(string decor) => ToggleLineStart(decor, false, false);

    #endregion

    #region Wrapping

    private void ToggleWrap(char decor, int count = 1, bool onlyWrap = false, bool shouldWrap = true) =>
        ToggleWrap(new string(decor, count), onlyWrap, shouldWrap);

    private void ToggleWrap(string decor, bool onlyWrap = false, bool shouldWrap = true)
    {
        int start = mdText.SelectionStart;
        int len = mdText.SelectionLength;

        ReadOnlySpan<char> span = mdText.Text.AsSpan();

        if (span.Length < start + len) return;

        var before = span[..start];
        var after = span[(start + len)..];
        var slice = span[start..(start + len)];

        var decorSpan = decor.AsSpan();
        int decorLen = decorSpan.Length;

        StringBuilder result = new();

        if (onlyWrap || !(before.Length >= decorLen && after.Length >= decorLen &&
            before[^decorLen..^0].SequenceEqual(decorSpan) &&
            after[0..decorLen].SequenceEqual(decorSpan)))
        {
            if (shouldWrap)
            {
                // Wrapping the selection.
                result.Append(before).Append(decorSpan).Append(slice).Append(decorSpan).Append(after);
                start += decorLen;
            }
        }
        else
        {
            // Unwrapping the selection.
            result.Append(before[..^decorLen]).Append(slice).Append(after[decorLen..]);
            start -= decorLen;
        }

        mdText.Text = result.ToString();
        mdText.Select(start, len);
    }

    private void WrapWith(char decor, int count = 1) => WrapWith(new(decor, count));

    private void WrapWith(string decor) => ToggleWrap(decor, true);

    private void UnWrap(char decor, int count = 1) => UnWrap(new(decor, count));

    private void UnWrap(string decor) => ToggleWrap(decor, false, false);

    #endregion

    private void BoldButton_Click(object sender, RoutedEventArgs e) => ToggleWrap('*', 2);

    private void ItalicButton_Click(object sender, RoutedEventArgs e) => ToggleWrap('*');

    private void StrikeButton_Click(object sender, RoutedEventArgs e) => ToggleWrap('~', 2);

    private void SubscriptButton_Click(object sender, RoutedEventArgs e) => ToggleWrap('~');

    private void SuperscriptButton_Click(object sender, RoutedEventArgs e) => ToggleWrap('^');

    private void IndentIncreaseButton_Click(object sender, RoutedEventArgs e) => InsertInFrontOfLines("    ");

    private void IndentDecreaseButton_Click(object sender, RoutedEventArgs e) => RemoveFromFrontOfLines("    ");

    private void QuoteButton_Click(object sender, RoutedEventArgs e) => InsertInFrontOfLines("> ");

    private void ULButton_Click(object sender, RoutedEventArgs e) => ToggleLineStarts("- ");

    private void OLButton_Click(object sender, RoutedEventArgs e) => ToggleLineStarts("1. ");

    private void TLButton_Click(object sender, RoutedEventArgs e) => ToggleLineStarts("- [x] ");

    private void CodeBlockButton_Click(object sender, RoutedEventArgs e) => ToggleWrap("```\r");

    private void CodeInlineButton_Click(object sender, RoutedEventArgs e) => ToggleWrap('`');

    private void ThBreakButton_Click(object sender, RoutedEventArgs e) => InsertInFrontOfLine("---\r");

    #endregion
}
