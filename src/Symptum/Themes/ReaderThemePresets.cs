using Windows.UI;

namespace Symptum.Themes;

public record ReaderThemePreset(
    string Name,
    Color Background,
    Color Foreground,
    Color Accent,
    Color CodeBackground,
    Color CodeBorder,
    Color TableHeaderForeground,
    Color TableBorder,
    Color TableAltBackground,
    Color QuoteBackground);

public static class ReaderThemePresets
{
    public static readonly ReaderThemePreset[] Presets =
    [
        new("Sepia",
            Background:       Color.FromArgb(255, 252, 247, 234),
            Foreground:       Color.FromArgb(255, 60, 40, 25),
            Accent:           Color.FromArgb(255, 150, 80, 30),
            CodeBackground:   Color.FromArgb(255, 240, 232, 216),
            CodeBorder:       Color.FromArgb(255, 212, 196, 160),
            TableHeaderForeground: Color.FromArgb(255, 255, 255, 255),
            TableBorder:      Color.FromArgb(255, 212, 196, 160),
            TableAltBackground: Color.FromArgb(255, 245, 237, 222),
            QuoteBackground:  Color.FromArgb(255, 245, 237, 222)),

        new("Warm Light",
            Background:       Color.FromArgb(255, 255, 250, 240),
            Foreground:       Color.FromArgb(255, 51, 51, 51),
            Accent:           Color.FromArgb(255, 140, 90, 40),
            CodeBackground:   Color.FromArgb(255, 245, 240, 230),
            CodeBorder:       Color.FromArgb(255, 220, 210, 195),
            TableHeaderForeground: Color.FromArgb(255, 255, 255, 255),
            TableBorder:      Color.FromArgb(255, 220, 210, 195),
            TableAltBackground: Color.FromArgb(255, 250, 245, 235),
            QuoteBackground:  Color.FromArgb(255, 250, 245, 235)),

        new("Nord",
            Background:       Color.FromArgb(255, 46, 52, 64),
            Foreground:       Color.FromArgb(255, 216, 222, 233),
            Accent:           Color.FromArgb(255, 136, 192, 208),
            CodeBackground:   Color.FromArgb(255, 59, 66, 82),
            CodeBorder:       Color.FromArgb(255, 76, 86, 106),
            TableHeaderForeground: Color.FromArgb(255, 46, 52, 64),
            TableBorder:      Color.FromArgb(255, 76, 86, 106),
            TableAltBackground: Color.FromArgb(255, 53, 59, 74),
            QuoteBackground:  Color.FromArgb(255, 59, 66, 82)),

        new("Dracula",
            Background:       Color.FromArgb(255, 40, 42, 54),
            Foreground:       Color.FromArgb(255, 248, 248, 242),
            Accent:           Color.FromArgb(255, 189, 147, 249),
            CodeBackground:   Color.FromArgb(255, 68, 71, 90),
            CodeBorder:       Color.FromArgb(255, 98, 114, 164),
            TableHeaderForeground: Color.FromArgb(255, 40, 42, 54),
            TableBorder:      Color.FromArgb(255, 98, 114, 164),
            TableAltBackground: Color.FromArgb(255, 50, 53, 70),
            QuoteBackground:  Color.FromArgb(255, 68, 71, 90)),

        new("OLED Dark",
            Background:       Color.FromArgb(255, 0, 0, 0),
            Foreground:       Color.FromArgb(255, 204, 204, 204),
            Accent:           Color.FromArgb(255, 100, 140, 230),
            CodeBackground:   Color.FromArgb(255, 20, 20, 20),
            CodeBorder:       Color.FromArgb(255, 50, 50, 50),
            TableHeaderForeground: Color.FromArgb(255, 255, 255, 255),
            TableBorder:      Color.FromArgb(255, 50, 50, 50),
            TableAltBackground: Color.FromArgb(255, 15, 15, 15),
            QuoteBackground:  Color.FromArgb(255, 20, 20, 20))
    ];
}
