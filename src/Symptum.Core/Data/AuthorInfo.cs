using System.Text.Json.Serialization;
using Symptum.Core.Serialization;

namespace Symptum.Core.Data;

[JsonConverter(typeof(AuthorInfoConverter))]
public struct AuthorInfo
{
    // This will be similar to git author information i.e. "Name <Email>"
    public string? Name { get; set; }

    public string? Email { get; set; }

    public override string ToString()
    {
        if (Name == null && Email == null)
            return string.Empty;
        else
            return string.Format("{0} <{1}>", Name, Email);
    }

    public static bool TryParse(string? value, out AuthorInfo authorInfo)
    {
        authorInfo = new();
        if (!string.IsNullOrWhiteSpace(value))
        {
            int emailStartIndex = value.IndexOf('<');
            if (emailStartIndex >= 0)
            {
                int emailEndIndex = value.IndexOf('>');
                if (emailEndIndex > emailStartIndex)
                {
                    authorInfo.Name = value[..emailStartIndex].Trim();
                    authorInfo.Email = value[(emailStartIndex + 1)..emailEndIndex];
                    return true;
                }
            }
        }

        return false;
    }
}
