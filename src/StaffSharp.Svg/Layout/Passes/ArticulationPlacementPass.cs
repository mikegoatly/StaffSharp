namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;

/// <summary>
/// Positions articulations and decorations on notes and chords.
/// Must run after StemAndBeamPass since it needs stem direction information.
/// </summary>
internal class ArticulationPlacementPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var staff in model.Systems.SelectMany(s => s.Staves))
        {
            foreach (var measure in staff.Measures)
            {
                ProcessMeasure(measure, staff, context);
            }
        }
    }

    private static void ProcessMeasure(LayoutMeasure measure, LayoutStaff staff, SvgContext context)
    {
        // Calculate staff baseline once for the measure
        double staffBaseline = staff.Bounds.Y + (2.0 * context.StaffSpace);
        double staffTopY = staff.Bounds.Y;

        foreach (var symbol in measure.Symbols)
        {
            switch (symbol)
            {
                case NoteLayoutSymbol noteSymbol:
                    ProcessNote(noteSymbol, staffBaseline, staffTopY, context);
                    break;

                case ChordLayoutSymbol chordSymbol:
                    ProcessChord(chordSymbol, staffBaseline, staffTopY, context);
                    break;
            }
        }
    }

    private static void ProcessNote(
        NoteLayoutSymbol noteSymbol,
        double staffBaseline,
        double staffTopY,
        SvgContext context)
    {
        var decorations = noteSymbol.Note.Decorations;
        if (decorations.Count == 0)
        {
            return;
        }

        var positionedDecorations = ArticulationCalculator.CalculateArticulations(
            noteSymbol,
            decorations,
            context,
            staffBaseline,
            staffTopY);

        foreach (var positioned in positionedDecorations)
        {
            noteSymbol.Decorations.Add(positioned);
        }
    }

    private static void ProcessChord(
        ChordLayoutSymbol chordSymbol,
        double staffBaseline,
        double staffTopY,
        SvgContext context)
    {
        var decorations = chordSymbol.Chord.Decorations;

        if (decorations.Count == 0)
        {
            return;
        }

        var positionedDecorations = ArticulationCalculator.CalculateArticulations(
            chordSymbol,
            decorations,
            context,
            staffBaseline,
            staffTopY);

        foreach (var positioned in positionedDecorations)
        {
            chordSymbol.Decorations.Add(positioned);
        }
    }
}
