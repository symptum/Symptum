using System.Xml;
using System.Xml.Serialization;

namespace Symptum.Common.ProjectSystem;

/// <summary>
/// Represents a simple project definition containing a collection of
/// <see cref="ProjectEntry"/> items. Projects can be serialized to and
/// deserialized from XML using the provided helpers.
/// </summary>
public class Project
{
    /// <summary>
    /// Gets or sets the name of the project. This property is ignored during XML serialization.
    /// </summary>
    [XmlIgnore]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the collection of project entries. Each entry represents a file to be included in the project.
    /// </summary>
    public List<ProjectEntry>? Entries { get; set; }

    private static readonly XmlSerializer _serializer = new(typeof(Project));

    /// <summary>
    /// Serializes the given <see cref="Project"/> instance to an XML string.
    /// </summary>
    /// <param name="project">Project instance to serialize.</param>
    /// <returns>XML representation of the project.</returns>
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

    /// <summary>
    /// Deserializes the given XML into a <see cref="Project"/> instance.
    /// </summary>
    /// <param name="xml">XML string containing the project data.</param>
    /// <returns>The deserialized <see cref="Project"/> or <c>null</c> when
    /// the input is invalid or deserialization failed.</returns>
    public static Project? Deserialize(string xml)
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

/// <summary>
/// Represents a single entry inside a project. Each entry maps a relative
/// folder path to a file name that should be included in the project.
/// </summary>
public class ProjectEntry
{
    /// <summary>
    /// Parameterless constructor used by serializers.
    /// </summary>
    public ProjectEntry() { }

    /// <summary>
    /// Creates a new project entry for the specified relative path and file
    /// name.
    /// </summary>
    /// <param name="path">Relative path under the project.</param>
    /// <param name="name">File name of the resource.</param>
    public ProjectEntry(string path, string name)
    {
        Path = path;
        Name = name;
    }

    /// <summary>
    /// Gets or sets the relative path under the project where the file is located.
    /// </summary>
    [XmlAttribute]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the file name of the resource to be included in the project.
    /// </summary>
    [XmlAttribute]
    public string? Name { get; set; }
}
