using System.Xml;
using System.Xml.Serialization;

namespace Symptum.Common.ProjectSystem;

public class Project
{
    [XmlIgnore]
    public string? Name { get; set; }

    public List<ProjectEntry>? Entries { get; set; }

    private static XmlSerializer _serializer = new(typeof(Project));

    public static string Serialize(Project project)
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

    public static Project? DeserializeProject(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;
        try
        {
            return (Project?)_serializer.Deserialize(new StringReader(xml));
        }
        catch { }
        return null;
    }
}

public class ProjectEntry
{
    public ProjectEntry() { }

    public ProjectEntry(string path, string name)
    {
        Path = path;
        Name = name;
    }

    [XmlAttribute]
    public string? Path { get; set; }

    [XmlAttribute]
    public string? Name { get; set; }
}
