// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//using ColorCode;
//using ColorCode.Common;
//using ColorCode.Styling;
using Markdig.Syntax.Inlines;
using System.Xml.Linq;
using System.Globalization;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Documents;
using System.Text.RegularExpressions;
using Windows.Foundation;
using System.Text;

/* Unmerged change from project 'Symptum.UI (net9.0-maccatalyst)'
Added:
using CommunityToolkit;
using CommunityToolkit.Labs;
using CommunityToolkit.Labs.WinUI;
using Symptum.UI.Markdown;
using Symptum.UI.Markdown;
*/

namespace Symptum.UI.Markdown;

public static class Extensions
{
    public const string Blue = "#FF0000FF";
    public const string White = "#FFFFFFFF";
    public const string Black = "#FF000000";
    public const string DullRed = "#FFA31515";
    public const string Yellow = "#FFFFFF00";
    public const string Green = "#FF008000";
    public const string PowderBlue = "#FFB0E0E6";
    public const string Teal = "#FF008080";
    public const string Gray = "#FF808080";
    public const string Navy = "#FF000080";
    public const string OrangeRed = "#FFFF4500";
    public const string Purple = "#FF800080";
    public const string Red = "#FFFF0000";
    public const string MediumTurqoise = "FF48D1CC";
    public const string Magenta = "FFFF00FF";
    public const string OliveDrab = "#FF6B8E23";
    public const string DarkOliveGreen = "#FF556B2F";
    public const string DarkCyan = "#FF008B8B";

    public static string ToAlphabetical(this int index)
    {
        string alphabetical = "abcdefghijklmnopqrstuvwxyz";
        int remainder = index;
        StringBuilder stringBuilder = new();
        while (remainder != 0)
        {
            if (remainder > 26)
            {
                int newRemainder = remainder % 26;
                int i = (remainder - newRemainder) / 26;
                stringBuilder.Append(alphabetical[i - 1]);
                remainder = newRemainder;
            }
            else
            {
                stringBuilder.Append(alphabetical[remainder - 1]);
                remainder = 0;
            }
        }
        return stringBuilder.ToString();
    }

    public static TextPointer? GetNextInsertionPosition(this TextPointer position, LogicalDirection logicalDirection)
    {
        // Check if the current position is already an insertion position
        if (position.IsAtInsertionPosition(logicalDirection))
        {
            // Return the same position
            return position;
        }
        else
        {
            // Try to find the next insertion position by moving one symbol forward
            TextPointer next = position.GetPositionAtOffset(1, logicalDirection);
            // If there is no next position, return null
            if (next == null)
            {
                return null;
            }
            else
            {
                // Recursively call this method until an insertion position is found or null is returned
                return next.GetNextInsertionPosition(logicalDirection);
            }
        }
    }

    public static bool IsAtInsertionPosition(this TextPointer position, LogicalDirection logicalDirection)
    {
        // Get the character rect of the current position
        Rect currentRect = position.GetCharacterRect(logicalDirection);
        // Try to get the next position by moving one symbol forward
        TextPointer next = position.GetPositionAtOffset(1, logicalDirection);
        // If there is no next position, return false
        if (next == null)
        {
            return false;
        }
        else
        {
            // Get the character rect of the next position
            Rect nextRect = next.GetCharacterRect(logicalDirection);
            // Compare the two rects and return true if they are different
            return !currentRect.Equals(nextRect);
        }
    }

    public static string RemoveImageSize(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("URL must not be null or empty", nameof(url));
        }

        // Create a regex pattern to match the URL with width and height
        string pattern = @"([^)\s]+)\s*=\s*\d+x\d+\s*";

        // Replace the matched URL with the URL only
        string result = Regex.Replace(url, pattern, "$1");

        return result;
    }

    public static Uri GetUri(string? url, string? @base)
    {
        string validUrl = RemoveImageSize(url);
        Uri result;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        if (Uri.TryCreate(validUrl, UriKind.Absolute, out result))
        {
            //the url is already absolute
            return result;
        }
        else if (!string.IsNullOrWhiteSpace(@base))
        {
            //the url is relative, so append the base
            //trim any trailing "/" from the base and any leading "/" from the url
            @base = @base?.TrimEnd('/');
            validUrl = validUrl.TrimStart('/');
            //return the base and the url separated by a single "/"
            return new Uri(@base + "/" + validUrl);
        }
        else
        {
            //the url is relative to the file system
            //add ms-appx
            validUrl = validUrl.TrimStart('/');
            return new Uri("ms-appx:///" + validUrl);
        }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }

    public static bool IsHeading(this string tag)
    {
        List<string> headings = new() { "h1", "h2", "h3", "h4", "h5", "h6" };
        return headings.Contains(tag.ToLower());
    }

    public static Size GetSvgSize(string svgString)
    {
        // Parse the SVG string as an XML document
        XDocument svgDocument = XDocument.Parse(svgString);

        // Get the root element of the document
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        XElement svgElement = svgDocument.Root;

        // Get the height and width attributes of the root element
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        XAttribute heightAttribute = svgElement.Attribute("height");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        XAttribute widthAttribute = svgElement.Attribute("width");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        // Convert the attribute values to double
        double.TryParse(heightAttribute?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out double height);
        double.TryParse(widthAttribute?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out double width);

        // Return the height and width as a tuple
        return new(width, height);
    }

    public static Size GetMarkdownImageSize(LinkInline link)
    {
        if (link == null || !link.IsImage)
        {
            throw new ArgumentException("Link must be an image", nameof(link));
        }

        string? url = link.Url;
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Link must have a valid URL", nameof(link));
        }

        // Try to parse the width and height from the URL
        string[]? parts = url?.Split('=');
        if (parts?.Length == 2)
        {
            string[] dimensions = parts[1].Split('x');
            if (dimensions.Length == 2 && int.TryParse(dimensions[0], out int width) && int.TryParse(dimensions[1], out int height))
            {
                return new(width, height);
            }
        }

        // not using this one as it's seems to be from the HTML renderer
        //// Try to parse the width and height from the special attributes
        //var attributes = link.GetAttributes();
        //if (attributes != null && attributes.Properties != null)
        //{
        //    var width = attributes.Properties.FirstOrDefault(p => p.Key == "width")?.Value;
        //    var height = attributes.Properties.FirstOrDefault(p => p.Key == "height")?.Value;
        //    if (!string.IsNullOrEmpty(width) && !string.IsNullOrEmpty(height) && int.TryParse(width, out int w) && int.TryParse(height, out int h))
        //    {
        //        return new(w, h);
        //    }
        //}

        // Return default values if no width and height are found
        return new(0, 0);
    }

    public static SolidColorBrush GetAccentColorBrush()
    {
        // Create a UISettings object to get the accent color
        UISettings uiSettings = new();

        // Get the accent color as a Color value
        Windows.UI.Color accentColor = uiSettings.GetColorValue(UIColorType.Accent);

        // Create a SolidColorBrush from the accent color
        SolidColorBrush accentBrush = new(accentColor);

        return accentBrush;
    }
}
