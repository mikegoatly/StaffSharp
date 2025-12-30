namespace StaffSharp.MusicXml;

using StaffSharp;
using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Score importer for MusicXML format (v4.0).
/// </summary>
public sealed class MusicXmlScoreImporter : IScoreImporter
{
    private readonly bool _enableValidation;

    /// <summary>
    /// Creates a new MusicXML score importer.
    /// </summary>
    /// <param name="enableValidation">Whether to validate the XML against the MusicXML schema before parsing.</param>
    public MusicXmlScoreImporter(bool enableValidation = true)
    {
        _enableValidation = enableValidation;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".xml", ".musicxml" };

    /// <inheritdoc/>
    public string FormatName => "MusicXML";

    /// <inheritdoc/>
    public async Task<NotationScore> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Load XML document
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        // TODO: Schema validation if enabled
        // if (_enableValidation)
        // {
        //     MusicXmlSchemaValidator.Validate(document);
        // }

        // Parse synchronously (XML parsing is fast)
        return MusicXmlParser.Parse(document);
    }
}
