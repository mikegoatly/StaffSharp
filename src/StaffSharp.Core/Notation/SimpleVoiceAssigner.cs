using StaffSharp.Performance;

namespace StaffSharp.Core.Notation;

/// <summary>
/// Simple voice assigner that uses voice hints when available, otherwise assigns based on pitch ranges and overlaps.
/// </summary>
public sealed class SimpleVoiceAssigner : IVoiceAssigner
{
    /// <inheritdoc/>
    public IReadOnlyList<VoiceAssignment> AssignVoices(IReadOnlyList<IPerformanceEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            return Array.Empty<VoiceAssignment>();
        }

        // Track voice number for each event (mutable so renumbering works)
        var eventVoiceNumbers = new Dictionary<IPerformanceEvent, int>(events.Count);
        var activeVoices = new List<VoiceState>();

        foreach (var @event in events)
        {
            // Remove voices that have finished before this event's onset
            activeVoices.RemoveAll(v => v.EndBeats <= @event.OnsetBeats);

            int voiceNumber;

            // Check if event has a voice hint
            var voiceHint = GetVoiceHint(@event);
            if (voiceHint.HasValue)
            {
                voiceNumber = voiceHint.Value;
            }
            else if (activeVoices.Count == 0)
            {
                // No active voices, start with voice 1
                voiceNumber = 1;
            }
            else
            {
                // Find best voice based on pitch similarity
                voiceNumber = FindBestVoice(@event, activeVoices, eventVoiceNumbers);
            }

            // Store the voice assignment for this event
            eventVoiceNumbers[@event] = voiceNumber;

            // Track this event's end time for the voice
            var endBeats = GetEventEndBeats(@event);
            var existingVoice = activeVoices.FirstOrDefault(v => v.VoiceNumber == voiceNumber);
            if (existingVoice != null)
            {
                existingVoice.EndBeats = existingVoice.EndBeats > endBeats ? existingVoice.EndBeats : endBeats;
                existingVoice.LastPitch = GetEventPitch(@event);
            }
            else
            {
                activeVoices.Add(new VoiceState
                {
                    VoiceNumber = voiceNumber,
                    EndBeats = endBeats,
                    LastPitch = GetEventPitch(@event),
                    Event = @event
                });
            }
        }

        // Convert dictionary to assignment list
        return events.Select(@event => new VoiceAssignment(@event, eventVoiceNumbers[@event])).ToList();
    }

    private static int? GetVoiceHint(IPerformanceEvent @event)
    {
        return @event switch
        {
            QuantizedNoteEvent qne => qne.VoiceHint,
            SymbolicNoteEvent sne => sne.VoiceHint,
            _ => null
        };
    }

    private static Rational GetEventEndBeats(IPerformanceEvent @event)
    {
        return @event switch
        {
            QuantizedNoteEvent qne => qne.OffsetBeats,
            SymbolicNoteEvent sne => sne.OnsetBeats + sne.DurationBeats,
            _ => @event.OnsetBeats
        };
    }

    private static MidiNote? GetEventPitch(IPerformanceEvent @event)
    {
        return @event switch
        {
            QuantizedNoteEvent qne => qne.RawEvent.Pitch,
            SymbolicNoteEvent sne => sne.Pitch,
            _ => null
        };
    }

    private static int FindBestVoice(
        IPerformanceEvent @event,
        List<VoiceState> activeVoices,
        Dictionary<IPerformanceEvent, int> eventVoiceNumbers)
    {
        // activeVoices contains voices that overlap with this event (haven't finished yet)
        // We should NEVER reuse an active voice - always create a new one for overlapping notes

        var pitch = GetEventPitch(@event);
        if (!pitch.HasValue)
        {
            // Non-note event, assign to next available voice
            return activeVoices.Max(v => v.VoiceNumber) + 1;
        }

        // Create new voice with correct numbering based on pitch
        // Higher pitches get lower voice numbers (soprano, alto, tenor, bass convention)
        var higherVoiceCount = activeVoices.Count(v =>
            v.LastPitch.HasValue &&
            v.LastPitch.Value.MidiNumber > pitch.Value.MidiNumber);

        // New voice number = number of voices above it + 1
        var newVoiceNumber = higherVoiceCount + 1;

        // Renumber existing voices that need to shift up
        foreach (var voice in activeVoices)
        {
            if (voice.VoiceNumber >= newVoiceNumber)
            {
                voice.VoiceNumber++;
                // Also update the event's voice assignment
                if (voice.Event != null)
                {
                    eventVoiceNumbers[voice.Event] = voice.VoiceNumber;
                }
            }
        }

        return newVoiceNumber;
    }

    private sealed class VoiceState
    {
        public required int VoiceNumber { get; set; } // Must be mutable for renumbering
        public required Rational EndBeats { get; set; }
        public MidiNote? LastPitch { get; set; }
        public IPerformanceEvent? Event { get; set; } // Track which event created this voice
    }
}
