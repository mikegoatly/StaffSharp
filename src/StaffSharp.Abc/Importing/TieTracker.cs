namespace StaffSharp.Abc.Importing;

using StaffSharp.Notation;

/// <summary>
/// Tracks tie state while parsing a measure and resolves tie endings in a post-processing pass.
/// Ties in ABC: C-C means the two C notes are tied together.
/// Tie chains: A-A-A means three A notes tied together (first=Start, middle=Both, last=End).
/// </summary>
internal static class TieTracker
{
    /// <summary>
    /// Post-processes events to mark tie endings.
    /// When a note/chord has TieType.Start, finds the next matching note/chord and marks it with TieType.End.
    /// </summary>
    public static void ResolveTieEndings(List<INotationEvent> events)
    {
        for (int i = 0; i < events.Count - 1; i++)
        {
            var current = events[i];

            // Check if current event has a tie start
            bool hasTieStart = current switch
            {
                NotationNote note => note.Tie == TieType.Start || note.Tie == TieType.Both,
                Chord chord => chord.Tie == TieType.Start || chord.Tie == TieType.Both,
                _ => false
            };

            if (!hasTieStart)
            {
                continue;
            }

            // Find next matching event and mark it with TieType.End (or TieType.Both if it also starts a tie)
            var next = events[i + 1];

            events[i + 1] = next switch
            {
                // Match notes with same pitch
                NotationNote nextNote when current is NotationNote currentNote &&
                                           nextNote.Pitch.Equals(currentNote.Pitch) =>
                    nextNote with
                    {
                        Tie = nextNote.Tie == TieType.Start ? TieType.Both : TieType.End
                    },

                // Match chords with same pitches
                Chord nextChord when current is Chord currentChord &&
                                     ChordsMatch(currentChord, nextChord) =>
                    new Chord(
                        nextChord.Pitches,
                        nextChord.Duration,
                        nextChord.Velocity,
                        nextChord.Tie == TieType.Start ? TieType.Both : TieType.End,
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
