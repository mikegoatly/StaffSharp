namespace StaffSharp.MusicXml;

using StaffSharp.Notation;

/// <summary>
/// Aggregates MusicXML slur start/stop markers across measures and emits SlurSpan records.
/// Keyed by (staff, voice, number) as per MusicXML semantics.
/// </summary>
internal sealed class SlurSpanAggregator
{
    private sealed record Key(int Staff, int Voice, int Number);

    private readonly Dictionary<Key, INotationEvent> _active = new();
    private readonly List<SlurSpan> _slurs = new();

    public void OnNote(INotationEvent noteEvent, int staffNumber, int voiceNumber, List<SlurInfo>? slurInfos)
    {
        if (slurInfos == null || slurInfos.Count == 0)
        {
            return;
        }

        foreach (var info in slurInfos)
        {
            var key = new Key(staffNumber, voiceNumber, info.Number);

            switch (info.Type)
            {
                case SlurType.Start:
                    // Only set if not already active for this number
                    if (!_active.ContainsKey(key))
                    {
                        _active[key] = noteEvent;
                    }
                    break;

                case SlurType.Stop:
                    if (_active.TryGetValue(key, out var startEvent))
                    {
                        // Only create if endpoints are meaningful for curves (note or chord)
                        if (IsNoteOrChord(startEvent) && IsNoteOrChord(noteEvent))
                        {
                            _slurs.Add(new SlurSpan(
                                StartEvent: startEvent,
                                EndEvent: noteEvent,
                                Number: info.Number,
                                IsDotted: false,
                                StartStaffNumber: staffNumber, // assume same staff unless cross-staff encountered later
                                EndStaffNumber: staffNumber,
                                StartVoiceNumber: voiceNumber,
                                EndVoiceNumber: voiceNumber));
                        }
                        _active.Remove(key);
                    }
                    break;
            }
        }
    }

    public List<SlurSpan> GetSlurs() => _slurs;

    private static bool IsNoteOrChord(INotationEvent e) => e is NotationNote || e is Chord;
}
