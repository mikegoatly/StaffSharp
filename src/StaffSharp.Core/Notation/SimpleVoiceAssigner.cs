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
            return [];
        }

        // Track voice number for each event (mutable so renumbering works)
        var eventVoiceNumbers = new Dictionary<IPerformanceEvent, int>(events.Count);
        var activeVoices = new List<VoiceState>();

        foreach (var evt in events)
        {
            // Remove voices that have finished before this event's onset
            activeVoices.RemoveAll(v => v.EndBeats <= evt.OnsetBeats);

            int voiceNumber;

            // Check if event has a voice hint

            if (evt.VoiceHint is not null)
            {
                voiceNumber = evt.VoiceHint.GetValueOrDefault();
            }
            else if (activeVoices.Count == 0)
            {
                // No active voices, start with voice 1
                voiceNumber = 1;
            }
            else
            {
                // Find best voice based on pitch similarity
                voiceNumber = FindBestVoice(evt, activeVoices, eventVoiceNumbers);
            }

            // Store the voice assignment for this event
            eventVoiceNumbers[evt] = voiceNumber;

            // Track this event's end time for the voice
            var endBeats = evt.OffsetBeats;
            var existingVoice = activeVoices.FirstOrDefault(v => v.VoiceNumber == voiceNumber);
            if (existingVoice != null)
            {
                existingVoice.EndBeats = existingVoice.EndBeats > endBeats ? existingVoice.EndBeats : endBeats;
                existingVoice.LastPitch = evt.Pitch;
            }
            else
            {
                activeVoices.Add(new VoiceState
                {
                    VoiceNumber = voiceNumber,
                    EndBeats = endBeats,
                    LastPitch = evt.Pitch,
                    Event = evt
                });
            }
        }

        // Convert dictionary to assignment list
        return events.Select(evt => new VoiceAssignment(evt, eventVoiceNumbers[evt])).ToList();
    }

    private static int FindBestVoice(
        IPerformanceEvent evt,
        List<VoiceState> activeVoices,
        Dictionary<IPerformanceEvent, int> eventVoiceNumbers)
    {
        // activeVoices contains voices that overlap with this event (haven't finished yet)
        // We should NEVER reuse an active voice - always create a new one for overlapping notes

        var pitch = evt.Pitch;

        // Create new voice with correct numbering based on pitch
        // Higher pitches get lower voice numbers (soprano, alto, tenor, bass convention)
        var higherVoiceCount = activeVoices.Count(v =>
            v.LastPitch.HasValue &&
            v.LastPitch.Value.MidiNumber > pitch.MidiNumber);

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
