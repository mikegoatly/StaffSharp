namespace StaffSharp.MusicXml;

using StaffSharp;
using StaffSharp.MusicXml.Validation;
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
    public async Task<NotationScore> ImportAsync(Stream stream, IProgress<ImportProgress>? progress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Load XML document
        progress?.Report(new ImportProgress(1, _enableValidation ? 3 : 2, "Loading MusicXML document"));
        var document = await XDocument.LoadAsync(stream, LoadOptions.SetLineInfo, cancellationToken).ConfigureAwait(false);

        // Validate against schema if enabled
        if (_enableValidation)
        {
            progress?.Report(new ImportProgress(2, 3, "Validating against MusicXML schema"));
            MusicXmlSchemaValidator.Validate(document);
        }

        // Parse synchronously (XML parsing is fast)
        var stepNumber = _enableValidation ? 3 : 2;
        var totalSteps = _enableValidation ? 3 : 2;
        progress?.Report(new ImportProgress(stepNumber, totalSteps, "Parsing MusicXML content"));
        return MusicXmlParser.Parse(document);
    }

    /// <inheritdoc/>
    public Task<NotationScore> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return ImportAsync(stream, progress: null, cancellationToken);
    }
}
