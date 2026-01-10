namespace StaffSharp.Abc.Importing;

using StaffSharp.Notation;

/// <summary>
/// Tracks tie state while parsing a measure and resolves tie endings in a post-processing pass.
/// Ties in ABC: C-C means the two C notes are tied together.
/// Tie chains: A-A-A means three A notes tied together (first has Start marker, middle has Start+Stop markers, last has Stop marker).
/// </summary>
internal static class TieTracker
{
    /// <summary>
    /// Post-processes events to mark tie endings.
    /// When a note/chord has TieMarker(Start), finds the next matching note/chord and adds TieMarker(Stop) to it.
    /// </summary>
    public static void ResolveTieEndings(List<INotationEvent> events)
    {
        for (int i = 0; i < events.Count - 1; i++)
        {
            var current = events[i];

            // Check if current event has a tie start marker (or Both for tie chains)
            bool hasTieStart = current switch
            {
                NotationNote note => note.TieMarker?.Type is TieMarkerType.Start or TieMarkerType.Both,
                Chord chord => chord.TieMarker?.Type is TieMarkerType.Start or TieMarkerType.Both,
                _ => false
            };

            if (!hasTieStart)
            {
                continue;
            }

            // Find next matching event and add Stop marker to it
            var next = events[i + 1];

            events[i + 1] = next switch
            {
                // Match notes with same pitch
                NotationNote nextNote when current is NotationNote currentNote &&
                                           nextNote.Pitch.Equals(currentNote.Pitch) =>
                    nextNote with
                    {
                        // If next note already has Start marker (tie chain), change to Both. Otherwise, set to Stop.
                        TieMarker = nextNote.TieMarker?.Type == TieMarkerType.Start
                            ? new TieMarker(TieMarkerType.Both)
                            : new TieMarker(TieMarkerType.Stop)
                    },

                // Match chords with same pitches
                Chord nextChord when current is Chord currentChord &&
                                     ChordsMatch(currentChord, nextChord) =>
                    new Chord(
                        nextChord.Pitches,
                        nextChord.Duration,
                        nextChord.Velocity,
                        // If next chord already has Start marker (tie chain), change to Both. Otherwise, set to Stop.
                        nextChord.TieMarker?.Type == TieMarkerType.Start
                            ? new TieMarker(TieMarkerType.Both)
                            : new TieMarker(TieMarkerType.Stop),
                        nextChord.GraceNote,
                        nextChord.Decorations,
                        nextChord.ChordSymbol,
                        nextChord.Annotation),

                _ => next
            };
        }
    }

    private static bool ChordsMatch(Chord a, Chord b)
    {
        if (a.Pitches.Count != b.Pitches.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Pitches.Count; i++)
        {
            if (!a.Pitches[i].Equals(b.Pitches[i]))
            {
                return false;
            }
        }

        return true;
    }
}
