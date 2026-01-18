using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using ScottPlot;
using ScottPlot.Avalonia;

namespace StaffSharp.Demo.Controls;

/// <summary>
/// A control that displays decoded MIDI notes as a piano roll.
/// Each note is rendered as a rectangle with optional velocity color coding.
/// </summary>
public class PianoRollVisualizationControl : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<StaffSharp.NoteEvent>?> NoteEventsProperty =
        AvaloniaProperty.Register<PianoRollVisualizationControl, IReadOnlyList<StaffSharp.NoteEvent>?>(nameof(NoteEvents));

    public static readonly StyledProperty<bool> ShowVelocityProperty =
        AvaloniaProperty.Register<PianoRollVisualizationControl, bool>(nameof(ShowVelocity), true);

    private readonly AvaPlot _plot;

    public IReadOnlyList<StaffSharp.NoteEvent>? NoteEvents
    {
        get => GetValue(NoteEventsProperty);
        set => SetValue(NoteEventsProperty, value);
    }

    public bool ShowVelocity
    {
        get => GetValue(ShowVelocityProperty);
        set => SetValue(ShowVelocityProperty, value);
    }

    public PianoRollVisualizationControl()
    {
        _plot = new AvaPlot();
        Content = _plot;
    }

    static PianoRollVisualizationControl()
    {
        NoteEventsProperty.Changed.AddClassHandler<PianoRollVisualizationControl>((x, _) => x.UpdatePlot());
        ShowVelocityProperty.Changed.AddClassHandler<PianoRollVisualizationControl>((x, _) => x.UpdatePlot());
    }

    private void UpdatePlot()
    {
        _plot.Plot.Clear();

        if (NoteEvents == null || NoteEvents.Count == 0)
        {
            _plot.Refresh();
            return;
        }

        // Find max time to calculate duration
        double maxTime = 0;
        foreach (var note in NoteEvents)
        {
            var endTime = note.Onset.TotalSeconds + note.Duration.TotalSeconds;
            if (endTime > maxTime)
                maxTime = endTime;
        }

        // Draw notes as rectangles
        foreach (var note in NoteEvents)
        {
            var x1 = note.Onset.TotalSeconds;
            var x2 = x1 + note.Duration.TotalSeconds;
            var y1 = note.Pitch.Value;
            var y2 = y1 + 0.8; // Leave small gap between notes (0.2 MIDI units)

            // Use opacity for velocity: loud notes = solid, soft notes = transparent
            var baseColor = Colors.DodgerBlue;
            var color = ShowVelocity
                ? baseColor.WithAlpha(note.Velocity.Value) // Velocity 0-1 maps to transparency
                : baseColor;

            var rect = _plot.Plot.Add.Rectangle(x1, x2, y1, y2);
            rect.FillColor = color;
            rect.LineWidth = 0;
        }

        // Add musical note grid lines (every octave at C notes)
        AddMusicalNoteGridLines();

        // Configure axes
        _plot.Plot.XLabel("Time (seconds)");
        _plot.Plot.YLabel("Note");
        _plot.Plot.Title("Piano Roll (Decoded Notes)");

        // Set default view to first 15 seconds (or full duration if shorter)
        // Note: Pan/zoom is enabled by default in ScottPlot.Avalonia
        var defaultViewDuration = Math.Min(15.0, maxTime);
        _plot.Plot.Axes.SetLimits(0, defaultViewDuration, 21, 109);

        _plot.Refresh();
    }

    private void AddMusicalNoteGridLines()
    {
        // Add horizontal grid lines at every C note (octaves)
        // MIDI notes: C1=24, C2=36, C3=48, C4=60 (middle C), C5=72, C6=84, C7=96, C8=108
        int[] cNotes = [24, 36, 48, 60, 72, 84, 96, 108];
        string[] labels = ["C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8"];

        for (int i = 0; i < cNotes.Length; i++)
        {
            var line = _plot.Plot.Add.HorizontalLine(cNotes[i]);
            line.Color = Colors.Gray.WithAlpha(0.3);
            line.LineWidth = 1;
            line.LinePattern = LinePattern.Dotted;
        }
    }
}
