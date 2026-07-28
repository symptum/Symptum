using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Symptum.Editor.Common;

// An XML based configuration for projects that will be created by the Editor in the root directory of the project.
// It will be created when a WorkFolder is selected regardless of a Project being used.
public class ProjectConfiguration
{
    public StringList ResourcesToOptimize { get; set; } = [];

    private static readonly XmlSerializer _serializer = new(typeof(ProjectConfiguration));

    public static string Serialize(ProjectConfiguration project)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = true
        };

        var namespaces = new XmlSerializerNamespaces([XmlQualifiedName.Empty]);
        using var stringWriter = new StringWriter();
        using XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings);
        _serializer.Serialize(xmlWriter, project, namespaces);
        return stringWriter.ToString();
    }

    public static ProjectConfiguration? Deserialize(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;
        try
        {
            return (ProjectConfiguration?)_serializer.Deserialize(new StringReader(xml));
        }
        catch { }
        return null;
    }

    public partial class StringList : List<string>, IXmlSerializable
    {
        public XmlSchema? GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            string content = reader.ReadElementContentAsString();
            Clear();

            if (!string.IsNullOrWhiteSpace(content))
            {
                foreach (var part in content.Split(';'))
                {
                    var trimmed = part.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        Add(trimmed);
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            for (int i = 0; i < Count; i++)
            {
                sb.Append("    ");
                sb.Append(this[i]);
                if (i < Count - 1) sb.Append(';');
                sb.AppendLine();
            }
            if (Count > 0) sb.Append("  ");
            writer.WriteString(sb.ToString());
        }
    }
}
