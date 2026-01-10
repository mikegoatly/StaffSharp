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

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                foreach (var measure in staff.Measures)
                {
                    ProcessMeasure(measure, context);
                }
            }
        }
    }

    private static void ProcessMeasure(LayoutMeasure measure, SvgContext context)
    {
        foreach (var symbol in measure.Symbols)
        {
            switch (symbol)
            {
                case NoteLayoutSymbol noteSymbol:
                    ProcessNote(noteSymbol, context);
                    break;

                case ChordLayoutSymbol chordSymbol:
                    ProcessChord(chordSymbol, context);
                    break;
            }
        }
    }

    private static void ProcessNote(NoteLayoutSymbol noteSymbol, SvgContext context)
    {
        var decorations = noteSymbol.Note.Decorations;
        if (decorations.Count == 0)
        {
            return;
        }

        var positionedDecorations = ArticulationCalculator.CalculateArticulations(
            noteSymbol,
            decorations,
            context);

        foreach (var positioned in positionedDecorations)
        {
            noteSymbol.PositionedDecorations.Add(positioned);
        }
    }

    private static void ProcessChord(ChordLayoutSymbol chordSymbol, SvgContext context)
    {
        var decorations = chordSymbol.Chord.Decorations;

        if (decorations.Count == 0)
        {
            return;
        }

        var positionedDecorations = ArticulationCalculator.CalculateArticulations(
            chordSymbol,
            decorations,
            context);

        foreach (var positioned in positionedDecorations)
        {
            chordSymbol.PositionedDecorations.Add(positioned);
        }
    }
}
