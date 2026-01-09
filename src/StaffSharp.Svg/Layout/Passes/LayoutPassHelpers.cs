namespace StaffSharp.Layout.Passes;

using StaffSharp.Layout.Model;
using StaffSharp.Notation;

/// <summary>
/// Helper methods for layout passes that process notation events and symbols.
/// </summary>
internal static class LayoutPassHelpers
{
    /// <summary>
    /// Builds event→symbol mappings for all staves in a system.
    /// Maps notation events to their corresponding layout symbols using reference equality.
    /// </summary>
    /// <param name="system">The layout system to build mappings for.</param>
    /// <returns>Dictionary mapping (PartIndex, StaffNumber) to event→symbol dictionaries.</returns>
    public static Dictionary<(int PartIndex, int StaffNumber), Dictionary<INotationEvent, IStemmedSymbol>>
        BuildEventSymbolMaps(LayoutSystem system)
    {
        var staffSymbolMaps = new Dictionary<(int PartIndex, int StaffNumber), Dictionary<INotationEvent, IStemmedSymbol>>();

        foreach (var staff in system.Staves)
        {
            var key = (staff.PartIndex, staff.StaffNumber);

            // Force reference comparison to match exact event instances
            var map = new Dictionary<INotationEvent, IStemmedSymbol>(ReferenceEqualityComparer<INotationEvent>.Instance);

            foreach (var symbol in staff.Measures.SelectMany(m => m.Symbols))
            {
                switch (symbol)
                {
                    case NoteLayoutSymbol n:
                        map[n.Note] = n;
                        break;
                    case ChordLayoutSymbol c:
                        map[c.Chord] = c;
                        break;
                }
            }

            staffSymbolMaps[key] = map;
        }

        return staffSymbolMaps;
    }
}
