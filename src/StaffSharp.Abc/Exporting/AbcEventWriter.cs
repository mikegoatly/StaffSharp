namespace StaffSharp.Abc.Exporting;

using System.Text;

using StaffSharp.Notation;

/// <summary>
/// Writes ABC notation events (notes, chords, rests).
/// </summary>
internal static class AbcEventWriter
{
    /// <summary>
    /// Number of measures to write per line for readability.
    /// </summary>
    private const int MeasuresPerLine = 4;

    /// <summary>
    /// Writes a single voice's measures to ABC notation.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="measures">The measures to write.</param>
    /// <param name="markerMap">Map of events to their tie/slur markers.</param>
    /// <param name="options">Export options.</param>
    public static void WriteMeasures(
        StringBuilder sb,
        IReadOnlyList<Measure> measures,
        Dictionary<INotationEvent, EventMarkers> markerMap,
        AbcExportOptions options)
    {
        // Track tuplet state across events
        Tuplet? activeTuplet = null;
        int tupletNotesRemaining = 0;

        for (int i = 0; i < measures.Count; i++)
        {
            var measure = measures[i];

            // Write repeat variant prefix if present (e.g., [1, [2)
            if (measure.RepeatVariants != null && measure.RepeatVariants.Count > 0)
            {
                sb.Append('[');
                for (int v = 0; v < measure.RepeatVariants.Count; v++)
                {
                    if (v > 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(measure.RepeatVariants[v]);
                }
                sb.Append(' '); // Space after variant
            }

            // Write start barline if present (for repeat starts)
            if (measure.StartBarline.HasValue && measure.StartBarline.Value == BarlineType.RepeatStart)
            {
                sb.Append("|:");
            }

            // Write events
            for (int e = 0; e < measure.Events.Count; e++)
            {
                WriteEvent(sb, measure.Events[e], markerMap, options, ref activeTuplet, ref tupletNotesRemaining);

                // Add space after event if not compact mode and not last event
                if (!options.CompactOutput && e < measure.Events.Count - 1)
                {
                    sb.Append(' ');
                }
            }

            // Write end barline
            var nextStartBarline = i + 1 < measures.Count ? measures[i + 1].StartBarline : null;
            sb.Append(AbcBarlineFormatter.Format(
                measure.EndBarline ?? BarlineType.Normal,
                nextStartBarline,
                null)); // Variants already written at start of measure

            // Add newline after certain number of measures for readability
            if ((i + 1) % MeasuresPerLine == 0 || i == measures.Count - 1)
            {
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Writes a single notation event (note, chord, or rest).
    /// </summary>
    private static void WriteEvent(
        StringBuilder sb,
        INotationEvent evt,
        Dictionary<INotationEvent, EventMarkers> markerMap,
        AbcExportOptions options,
        ref Tuplet? activeTuplet,
        ref int tupletNotesRemaining)
    {
        // Get tuplet from event duration (if any)
        var eventTuplet = evt.Duration.Tuplet;

        // Check if we need to start a new tuplet group
        if (eventTuplet != null && (activeTuplet == null || !eventTuplet.Equals(activeTuplet) || tupletNotesRemaining <= 0))
        {
            // Starting a new tuplet group - write tuplet specifier
            WriteTupletSpecifier(sb, eventTuplet);
            activeTuplet = eventTuplet;
            // Count how many consecutive notes have this tuplet
            tupletNotesRemaining = eventTuplet.ActualNotes;
        }

        // Get markers for this event
        markerMap.TryGetValue(evt, out var markers);

        // Write opening slurs (before the note)
        if (markers?.SlurStarts.Count > 0)
        {
            foreach (var slur in markers.SlurStarts)
            {
                if (slur.IsDotted)
                {
                    sb.Append(".(");
                }
                else
                {
                    sb.Append('(');
                }
            }
        }

        // Write the event itself
        switch (evt)
        {
            case NotationNote note:
                WriteNote(sb, note, options);
                break;

            case Chord chord:
                WriteChord(sb, chord, options);
                break;

            case Rest rest:
                WriteRest(sb, rest, options);
                break;
        }

        // Write tie marker (after the note)
        if (markers?.HasTie == true)
        {
            sb.Append('-');
        }

        // Write closing slurs (after the note)
        if (markers?.SlurEnds.Count > 0)
        {
            foreach (var _ in markers.SlurEnds)
            {
                sb.Append(')');
            }
        }

        // Decrement tuplet counter if we're in a tuplet
        if (eventTuplet != null && tupletNotesRemaining > 0)
        {
            tupletNotesRemaining--;
            if (tupletNotesRemaining <= 0)
            {
                activeTuplet = null;
            }
        }
    }

    /// <summary>
    /// Writes decorations and grace notes to ABC notation.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="decorations">The decorations to write.</param>
    /// <param name="graceNote">The grace note to write (if present).</param>
    private static void WriteDecorationsAndGraceNotes(
        StringBuilder sb,
        IReadOnlyList<Decoration> decorations,
        GraceNote? graceNote)
    {
        // Write decorations (before grace notes and pitch)
        foreach (var decoration in decorations)
        {
            sb.Append(AbcDecorationFormatter.Format(decoration));
        }

        // Write grace note (before main pitch)
        if (graceNote.HasValue)
        {
            sb.Append(AbcGraceNoteFormatter.Format(graceNote.Value));
        }
    }

    /// <summary>
    /// Writes a tuplet specifier (e.g., (3 or (3:2).
    /// </summary>
    private static void WriteTupletSpecifier(StringBuilder sb, Tuplet tuplet)
    {
        sb.Append('(');
        sb.Append(tuplet.ActualNotes);

        // Write :NormalNotes if it doesn't match the default
        var defaultNormalNotes = AbcTupletHelper.GetDefaultNormalNotes(tuplet.ActualNotes);
        if (tuplet.NormalNotes != defaultNormalNotes)
        {
            sb.Append(':');
            sb.Append(tuplet.NormalNotes);
        }
    }

    private static void WriteNote(
        StringBuilder sb,
        NotationNote note,
        AbcExportOptions options)
    {
        // Write decorations and grace notes
        WriteDecorationsAndGraceNotes(sb, note.Decorations, note.GraceNote);

        // Write pitch
        sb.Append(AbcPitchFormatter.Format(note.Pitch));

        // Write duration modifier
        var durationModifier = AbcDurationFormatter.Format(note.Duration, options.DefaultNoteLength);
        sb.Append(durationModifier);
    }

    private static void WriteChord(
        StringBuilder sb,
        Chord chord,
        AbcExportOptions options)
    {
        // Write decorations and grace notes
        WriteDecorationsAndGraceNotes(sb, chord.Decorations, chord.GraceNote);

        // Write opening bracket
        sb.Append('[');

        // Write all pitches
        foreach (var pitch in chord.Pitches)
        {
            sb.Append(AbcPitchFormatter.Format(pitch));
        }

        // Write closing bracket
        sb.Append(']');

        // Write duration modifier after the chord
        var durationModifier = AbcDurationFormatter.Format(chord.Duration, options.DefaultNoteLength);
        sb.Append(durationModifier);
    }

    private static void WriteRest(
        StringBuilder sb,
        Rest rest,
        AbcExportOptions options)
    {
        // Write rest symbol (lowercase z)
        sb.Append('z');

        // Write duration modifier
        var durationModifier = AbcDurationFormatter.Format(rest.Duration, options.DefaultNoteLength);
        sb.Append(durationModifier);
    }
}
