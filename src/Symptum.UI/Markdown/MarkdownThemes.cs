// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI;
using Microsoft.UI.Text;
using Windows.UI.Text;

namespace Symptum.UI.Markdown;

public sealed partial class MarkdownThemes : DependencyObject
{
    internal static MarkdownThemes Default { get; } = new();

    #region Common

    public Thickness Padding { get; set; } = new(36);

    public double Spacing { get; set; } = 12.0;

    public CornerRadius CornerRadius { get; set; } = new(4);

    #endregion

    #region Header Theme

    public double H1FontSize { get; set; } = 28;

    public double H2FontSize { get; set; } = 24;

    public double H3FontSize { get; set; } = 22;

    public double H4FontSize { get; set; } = 20;

    public double H5FontSize { get; set; } = 18;

    public double H6FontSize { get; set; } = 16;

    public Brush? H1Foreground { get; set; }

    public Brush? H2Foreground { get; set; }

    public Brush? H3Foreground { get; set; }

    public Brush? H4Foreground { get; set; }

    public Brush? H5Foreground { get; set; }

    public Brush? H6Foreground { get; set; }

    public FontWeight H1FontWeight { get; set; } = FontWeights.Bold;

    public FontWeight H2FontWeight { get; set; } = FontWeights.Normal;

    public FontWeight H3FontWeight { get; set; } = FontWeights.Bold;

    public FontWeight H4FontWeight { get; set; } = FontWeights.Normal;

    public FontWeight H5FontWeight { get; set; } = FontWeights.Bold;

    public FontWeight H6FontWeight { get; set; } = FontWeights.Normal;

    #endregion

    #region Code

    public Thickness CodeBlockPadding { get; set; } = new(8);

    public Thickness InternalMargin { get; set; } = new(4);

    public Brush InlineCodeBackground { get; set; } = (Brush)Application.Current.Resources["ExpanderHeaderBackground"];

    public Brush InlineCodeBorderBrush { get; set; } = new SolidColorBrush(Colors.Gray);

    public Thickness InlineCodeBorderThickness { get; set; } = new(1);

    public CornerRadius InlineCodeCornerRadius { get; set; } = new(2);

    public Thickness InlineCodePadding { get; set; } = new(4, 0, 4, 0);

    public double InlineCodeFontSize { get; set; } = 14;

    public FontWeight InlineCodeFontWeight { get; set; } = FontWeights.Normal;

    #endregion

    #region Quote

    public Style? DefaultQuoteBlockStyle { get; set; } = Application.Current.Resources.ContainsKey("DefaultQuoteBlockStyle") ?
        Application.Current.Resources["DefaultQuoteBlockStyle"] as Style : null;

    public Style? NoteQuoteBlockStyle { get; set; } = Application.Current.Resources.ContainsKey("NoteQuoteBlockStyle") ?
        Application.Current.Resources["NoteQuoteBlockStyle"] as Style : null;

    public Style? TipQuoteBlockStyle { get; set; } = Application.Current.Resources.ContainsKey("TipQuoteBlockStyle") ?
        Application.Current.Resources["TipQuoteBlockStyle"] as Style : null;

    public Style? WarningQuoteBlockStyle { get; set; } = Application.Current.Resources.ContainsKey("WarningQuoteBlockStyle") ?
        Application.Current.Resources["WarningQuoteBlockStyle"] as Style : null;

    public Style? CautionQuoteBlockStyle { get; set; } = Application.Current.Resources.ContainsKey("CautionQuoteBlockStyle") ?
        Application.Current.Resources["CautionQuoteBlockStyle"] as Style : null;

    #endregion

    #region List

    public Thickness ListMargin { get; set; } = new(16, 8, 0, 8);

    public double ListBulletSpacing { get; set; } = 12;

    #endregion

    public MarkdownThemes()
    {
        H1Foreground = H6Foreground = Symptum.UI.Markdown.Extensions.GetAccentColorBrush();
    }
}
