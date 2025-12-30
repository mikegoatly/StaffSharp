namespace StaffSharp.MusicXml;

using StaffSharp;
using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Parses metadata from MusicXML documents.
/// </summary>
internal static class MusicXmlMetadataParser
{
    /// <summary>
    /// Parses score metadata from the root element.
    /// </summary>
    public static ScoreMetadata ParseMetadata(XElement root)
    {
        // Parse title from work/work-title or movement-title
        string? title = root.Element("work")?.Element("work-title")?.Value
            ?? root.Element("movement-title")?.Value
            ?? "Untitled";

        // Parse composer from identification/creator[@type="composer"]
        string? composer = root.Element("identification")
            ?.Elements("creator")
            ?.FirstOrDefault(e => e.Attribute("type")?.Value == "composer")
            ?.Value;

        // Default values (will be overridden by first measure's attributes)
        var keySignature = KeySignature.C;
        var timeSignature = TimeSignature.CommonTime;
        int tempo = 120;

        return new ScoreMetadata(title, composer, keySignature, timeSignature, tempo);
    }

    /// <summary>
    /// Parses the part-list to extract part information.
    /// </summary>
    public static List<PartInfo> ParsePartList(XElement partList)
    {
        var parts = new List<PartInfo>();

        foreach (var scorePart in partList.Elements("score-part"))
        {
            var id = scorePart.Attribute("id")?.Value
                ?? throw new InvalidOperationException("score-part is missing id attribute.");

            var name = scorePart.Element("part-name")?.Value ?? "Unknown";

            parts.Add(new PartInfo(id, name));
        }

        return parts;
    }
}

/// <summary>
/// Holds information about a part from the part-list.
/// </summary>
internal sealed record PartInfo(string Id, string Name);
