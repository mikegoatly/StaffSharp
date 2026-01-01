namespace StaffSharp.MusicXml;

using StaffSharp;
using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Main parser for MusicXML documents.
/// </summary>
internal static class MusicXmlParser
{
    /// <summary>
    /// Parses a MusicXML document into a NotationScore.
    /// </summary>
    /// <param name="document">The XDocument to parse.</param>
    /// <returns>A NotationScore representation of the MusicXML document.</returns>
    /// <exception cref="NotSupportedException">Thrown when the document format is not supported.</exception>
    public static NotationScore Parse(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = document.Root ?? throw new InvalidOperationException("Document has no root element.");

        // Only support score-partwise for now
        if (root.Name.LocalName != "score-partwise")
        {
            throw new NotSupportedException(
                $"Only score-partwise format is supported. Found: {root.Name.LocalName}. " +
                "Please convert score-timewise documents to score-partwise format.");
        }

        // Parse metadata (work, movement-title, identification)
        var metadata = MusicXmlMetadataParser.ParseMetadata(root);

        // Parse part-list to get part information
        var partList = root.Element("part-list")
            ?? throw new InvalidOperationException("Document is missing required part-list element.");

        var scoreParts = MusicXmlMetadataParser.ParsePartList(partList);

        // Parse each part
        var parts = new List<Part>();
        foreach (var partElement in root.Elements("part"))
        {
            var partId = partElement.Attribute("id")?.Value
                ?? throw new InvalidOperationException("Part element is missing id attribute.");

            // Find the score-part metadata
            var scorePartInfo = scoreParts.FirstOrDefault(sp => sp.Id == partId);
            var partName = scorePartInfo?.Name ?? "Unknown";

            // Create a new context for this part
            var context = new MusicXmlContext
            {
                KeySignature = metadata.KeySignature,
                TimeSignature = metadata.TimeSignature,
                Tempo = metadata.Tempo
            };

            // Parse the part's measures into staves
            var staves = MusicXmlPartParser.ParsePart(partElement, context);

            parts.Add(new Part(partName, staves));
        }

        return new NotationScore(metadata, parts);
    }
}
