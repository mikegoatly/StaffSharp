using ScottPlot;
using ScottPlot.Plottables;

namespace StaffSharp.Demo.Controls;

/// <summary>
/// Extension methods for configuring ScottPlot plots with musical notation features.
/// </summary>
internal static class PlotExtensions
{
    /// <summary>
    /// Adds horizontal grid lines at every C note (octave markers).
    /// </summary>
    public static void AddMusicalNoteGridLines(this Plot plot)
    {
        // Add horizontal grid lines at every C note (octaves)
        // MIDI notes: C1=24, C2=36, C3=48, C4=60 (middle C), C5=72, C6=84, C7=96, C8=108
        int[] cNotes = [24, 36, 48, 60, 72, 84, 96, 108];

        foreach (var note in cNotes)
        {
            var line = plot.Add.HorizontalLine(note);
            line.Color = Colors.Gray.WithAlpha(0.3);
            line.LineWidth = 1;
            line.LinePattern = LinePattern.Dotted;
        }
    }

    /// <summary>
    /// Sets Y-axis tick labels to show note names (C1, C2, etc.) at octave markers.
    /// </summary>
    public static void SetMusicalNoteTickLabels(this Plot plot)
    {
        // Set Y-axis tick labels to show note names at octave markers
        // MIDI notes: C1=24, C2=36, C3=48, C4=60 (middle C), C5=72, C6=84, C7=96, C8=108
        double[] tickPositions = [24, 36, 48, 60, 72, 84, 96, 108];
        string[] tickLabels = ["C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8"];

        plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual(tickPositions, tickLabels);
    }

    /// <summary>
    /// Configures a piano roll heatmap with musical note gridlines, tick labels, and proper extent mapping.
    /// </summary>
    /// <param name="plot">The plot to configure.</param>
    /// <param name="heatmap">The heatmap plottable to configure.</param>
    /// <param name="title">The plot title.</param>
    /// <param name="duration">The duration in seconds for the X-axis extent.</param>
    public static void ConfigurePianoRollHeatmap(this Plot plot, Heatmap heatmap, string title, double duration)
    {
        heatmap.Colormap = new ScottPlot.Colormaps.Magma();

        // Map heatmap to time in seconds (X: 0 to duration, Y: MIDI 109-21)
        // Y is reversed (109 to 21) so low notes are at bottom, high notes at top
        // Data resolution: timeFrames columns × 88 rows mapped to duration seconds × 88 notes
        heatmap.Extent = new CoordinateRect(0, duration, 109, 21);

        plot.Title(title);
        plot.YLabel("Note");
        plot.AddMusicalNoteGridLines();
        plot.SetMusicalNoteTickLabels();
    }
}
