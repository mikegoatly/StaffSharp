namespace StaffSharp.MusicXml;

using StaffSharp.Notation;

/// <summary>
/// Aggregates MusicXML tie start/stop markers across measures and emits TieSpan records.
/// Keyed by (staff, voice, pitch) as ties connect notes of the same pitch.
/// </summary>
internal sealed class TieSpanAggregator
{
    // For ties, we key by staff, voice, and pitch (since ties must connect same-pitch notes)
    private sealed record Key(int Staff, int Voice, Pitch Pitch);

    private sealed record PendingTie(INotationEvent Event, int StaffNumber, int VoiceNumber);

    private readonly Dictionary<Key, PendingTie> _active = [];
    private readonly List<TieSpan> _ties = [];

    public void OnNote(INotationEvent noteEvent, int staffNumber, int voiceNumber, TieMarkerType? tieInfo)
    {
        if (tieInfo is null)
        {
            return;
        }

        // Get pitch for keying (ties must connect same pitch)
        Pitch? pitch = noteEvent switch
        {
            NotationNote note => note.Pitch,
            Chord chord => chord.Pitches.Count > 0 ? chord.Pitches[0] : null, // Use first pitch for chord ties
            _ => null
        };

        if (pitch is null)
        {
            return;
        }

        var key = new Key(staffNumber, voiceNumber, pitch.GetValueOrDefault());

        switch (tieInfo)
        {
            case TieMarkerType.Start:
                // Only set if not already active
                if (!_active.ContainsKey(key))
                {
                    _active[key] = new PendingTie(noteEvent, staffNumber, voiceNumber);
                }
                break;

            case TieMarkerType.Stop:
                if (_active.TryGetValue(key, out var pending))
                {
                    // Only create if endpoints are meaningful for ties (note or chord)
                    if (IsNoteOrChord(pending.Event) && IsNoteOrChord(noteEvent))
                    {
                        _ties.Add(new TieSpan(
                            StartEvent: pending.Event,
                            EndEvent: noteEvent,
                            StartStaffNumber: pending.StaffNumber,
                            EndStaffNumber: staffNumber,
                            StartVoiceNumber: pending.VoiceNumber,
                            EndVoiceNumber: voiceNumber));
                    }

                    _active.Remove(key);
                }
                break;

            case TieMarkerType.Both:
                // Both handles tie chains: end previous tie and start new tie
                if (_active.TryGetValue(key, out var pendingBoth))
                {
                    // End the previous tie
                    if (IsNoteOrChord(pendingBoth.Event) && IsNoteOrChord(noteEvent))
                    {
                        _ties.Add(new TieSpan(
                            StartEvent: pendingBoth.Event,
                            EndEvent: noteEvent,
                            StartStaffNumber: pendingBoth.StaffNumber,
                            EndStaffNumber: staffNumber,
                            StartVoiceNumber: pendingBoth.VoiceNumber,
                            EndVoiceNumber: voiceNumber));
                    }
                }
                // Start new tie from this note
                _active[key] = new PendingTie(noteEvent, staffNumber, voiceNumber);
                break;
        }
    }

    public List<TieSpan> GetTies() => _ties;

    private static bool IsNoteOrChord(INotationEvent e) => e is NotationNote || e is Chord;
}