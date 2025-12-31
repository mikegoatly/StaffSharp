namespace StaffSharp.MusicXml.Validation;

using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

/// <summary>
/// Validates MusicXML documents against the MusicXML 4.0 XSD schema.
/// </summary>
public static class MusicXmlSchemaValidator
{
    private static readonly Lazy<XmlSchemaSet> _schemaSet = new(LoadSchemas);

    /// <summary>
    /// Validates a MusicXML document against the schema.
    /// </summary>
    /// <param name="document">The XML document to validate.</param>
    /// <exception cref="MusicXmlValidationException">Thrown when the document is invalid.</exception>
    public static void Validate(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var errors = new List<string>();

        document.Validate(_schemaSet.Value, (sender, e) =>
        {
            var message = e.Severity == XmlSeverityType.Warning
                ? $"Warning: {e.Message}"
                : $"Error: {e.Message}";

            if (e.Exception?.LineNumber > 0)
            {
                message += $" (Line {e.Exception.LineNumber}, Position {e.Exception.LinePosition})";
            }

            errors.Add(message);
        });

        if (errors.Count > 0)
        {
            throw new MusicXmlValidationException(
                $"MusicXML document validation failed with {errors.Count} error(s).",
                errors);
        }
    }

    /// <summary>
    /// Validates a MusicXML document stream against the schema.
    /// </summary>
    /// <param name="stream">The stream containing the XML document.</param>
    /// <exception cref="MusicXmlValidationException">Thrown when the document is invalid.</exception>
    public static async Task ValidateAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var document = await XDocument.LoadAsync(stream, LoadOptions.SetLineInfo, CancellationToken.None)
            .ConfigureAwait(false);

        // Reset stream position for subsequent reading
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        Validate(document);
    }

    private static XmlSchemaSet LoadSchemas()
    {
        var schemaSet = new XmlSchemaSet();
        var assembly = typeof(MusicXmlSchemaValidator).Assembly;

        // Load the main MusicXML schema and its dependencies
        var schemaFiles = new[]
        {
            "StaffSharp.MusicXml.Schemas.xml.xsd",
            "StaffSharp.MusicXml.Schemas.xlink.xsd",
            "StaffSharp.MusicXml.Schemas.musicxml.xsd"
        };

        foreach (var schemaFile in schemaFiles)
        {
            using var stream = assembly.GetManifestResourceStream(schemaFile) 
                ?? throw new InvalidOperationException(
                    $"Failed to load embedded schema resource: {schemaFile}. " +
                    "Ensure the schema files are marked as EmbeddedResource.");
            using var reader = XmlReader.Create(stream);
            schemaSet.Add(null, reader);
        }

        // Compile the schema set
        schemaSet.Compile();

        return schemaSet;
    }
}
