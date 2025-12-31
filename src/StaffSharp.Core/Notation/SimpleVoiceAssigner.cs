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

        var assignments = new List<VoiceAssignment>(events.Count);
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
                voiceNumber = FindBestVoice(@event, activeVoices);
            }

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
                    LastPitch = GetEventPitch(@event)
                });
            }

            assignments.Add(new VoiceAssignment(@event, voiceNumber));
        }

        return assignments;
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

    private static int FindBestVoice(IPerformanceEvent @event, List<VoiceState> activeVoices)
    {
        var pitch = GetEventPitch(@event);
        if (!pitch.HasValue)
        {
            // Non-note event, assign to next available voice
            return activeVoices.Max(v => v.VoiceNumber) + 1;
        }

        // Find voice with closest pitch
        VoiceState? closestVoice = null;
        int minDistance = int.MaxValue;

        foreach (var voice in activeVoices)
        {
            if (voice.LastPitch.HasValue)
            {
                var distance = Math.Abs(pitch.Value.MidiNumber - voice.LastPitch.Value.MidiNumber);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestVoice = voice;
                }
            }
        }

        // If closest voice is within an octave, use it; otherwise create new voice
        if (closestVoice != null && minDistance <= 12)
        {
            return closestVoice.VoiceNumber;
        }

        // Create new voice - assign higher pitches to lower voice numbers
        var newVoiceNumber = activeVoices.Max(v => v.VoiceNumber) + 1;
        var higherVoicesList = activeVoices.Where(v => v.LastPitch.HasValue && v.LastPitch.Value.MidiNumber > pitch.Value.MidiNumber).ToList();
        if (higherVoicesList.Count > 0)
        {
            newVoiceNumber = higherVoicesList.Min(v => v.VoiceNumber);
        }

        return newVoiceNumber;
    }

    private sealed class VoiceState
    {
        public required int VoiceNumber { get; init; }
        public required Rational EndBeats { get; set; }
        public MidiNote? LastPitch { get; set; }
    }
}
