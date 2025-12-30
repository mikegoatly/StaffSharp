namespace StaffSharp.MusicXml;

using StaffSharp.Notation;
using System.Xml.Linq;

/// <summary>
/// Parses MusicXML barline elements.
/// </summary>
internal static class MusicXmlBarlineParser
{
    /// <summary>
    /// Parses a barline element and returns barline type and repeat variants.
    /// </summary>
    public static (BarlineType? BarlineType, List<int>? RepeatVariants) ParseBarline(XElement barlineElement)
    {
        ArgumentNullException.ThrowIfNull(barlineElement);

        var location = barlineElement.Attribute("location")?.Value;
        BarlineType? barlineType = null;
        List<int>? repeatVariants = null;

        // Check for repeat
        var repeatElement = barlineElement.Element("repeat");
        if (repeatElement != null)
        {
            var direction = repeatElement.Attribute("direction")?.Value;
            barlineType = direction switch
            {
                "forward" => BarlineType.RepeatStart,
                "backward" => BarlineType.RepeatEnd,
                _ => null
            };
        }

        // Check for bar-style if no repeat
        if (barlineType == null)
        {
            var barStyleElement = barlineElement.Element("bar-style");
            if (barStyleElement != null)
            {
                barlineType = barStyleElement.Value switch
                {
                    "light-heavy" => BarlineType.Final,
                    "light-light" => BarlineType.DoubleBar,
                    "heavy-light" => BarlineType.RepeatStart,
                    "heavy-heavy" => BarlineType.RepeatBoth,
                    _ => BarlineType.Normal
                };
            }
        }

        // Check for ending (repeat variants like |1. |2.)
        var endingElement = barlineElement.Element("ending");
        if (endingElement != null)
        {
            var numberAttr = endingElement.Attribute("number");
            if (numberAttr != null)
            {
                // Parse comma-separated numbers (e.g., "1,3" for first and third endings)
                var numbers = numberAttr.Value.Split(',', StringSplitOptions.TrimEntries);
                repeatVariants = new List<int>();
                foreach (var numStr in numbers)
                {
                    if (int.TryParse(numStr, out var num))
                    {
                        repeatVariants.Add(num);
                    }
                }
            }
        }

        return (barlineType, repeatVariants);
    }
}
