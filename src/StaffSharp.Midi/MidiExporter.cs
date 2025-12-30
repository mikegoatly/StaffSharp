namespace StaffSharp.Midi;

using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using StaffSharp;
using StaffSharp.Notation;
using MidiNote = Melanchall.DryWetMidi.Interaction.Note;

/// <summary>
/// Exports a NotationScore to MIDI format.
/// </summary>
public sealed class MidiExporter
{
    /// <summary>
    /// Exports a NotationScore to MIDI format, writing to the provided stream.
    /// </summary>
    /// <param name="score">The notation score to export.</param>
    /// <param name="stream">The stream to write the MIDI data to.</param>
    /// <param name="options">Optional export options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ExportAsync(
        NotationScore score,
        Stream stream,
        MidiExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(stream);

        options ??= new MidiExportOptions();

        // Create MIDI file with time division
        var midiFile = new MidiFile
        {
            TimeDivision = new TicksPerQuarterNoteTimeDivision((short)options.TicksPerQuarterNote)
        };

        // Create Track 0 with meta events (tempo, time signature, key signature)
        var metaTrack = new TrackChunk();
        AddMetaEvents(metaTrack, score.Metadata, options);
        midiFile.Chunks.Add(metaTrack);

        // Create one track per voice across all parts
        foreach (var part in score.Parts)
        {
            foreach (var voice in part.Voices)
            {
                var voiceTrack = new TrackChunk();
                midiFile.Chunks.Add(voiceTrack); // Add track BEFORE managing notes

                // Flatten all events from all measures into a single list with absolute timing
                var timedEvents = FlattenMeasures(voice.Measures);

                // Process events and handle ties
                var midiNotes = ProcessEvents(timedEvents, options);

                // Convert notes to NoteOn/NoteOff events with delta times
                var allEvents = new List<(long AbsoluteTime, MidiEvent Event)>();

                foreach (var note in midiNotes)
                {
                    var noteOn = new NoteOnEvent(note.NoteNumber, note.Velocity);
                    noteOn.Channel = note.Channel;
                    allEvents.Add((note.Time, noteOn));

                    var noteOff = new NoteOffEvent(note.NoteNumber, (SevenBitNumber)64); // Standard NoteOff velocity
                    noteOff.Channel = note.Channel;
                    allEvents.Add((note.Time + note.Length, noteOff));
                }

                // Sort by absolute time
                allEvents = allEvents.OrderBy(e => e.AbsoluteTime).ToList();

                // Convert to delta times and add to track
                long previousTime = 0;
                foreach (var (absoluteTime, midiEvent) in allEvents)
                {
                    midiEvent.DeltaTime = absoluteTime - previousTime;
                    voiceTrack.Events.Add(midiEvent);
                    previousTime = absoluteTime;
                }
            }
        }

        // Write to stream (DryWetMidi's Write is synchronous)
        midiFile.Write(stream);

        // Flush asynchronously
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddMetaEvents(TrackChunk track, ScoreMetadata metadata, MidiExportOptions options)
    {
        var events = track.Events;

        // Add tempo event (convert BPM to microseconds per quarter note)
        // Formula: microseconds = 60,000,000 / BPM
        long microsecondsPerQuarterNote = 60_000_000 / metadata.Tempo;
        events.Add(new SetTempoEvent(microsecondsPerQuarterNote));

        // Add time signature event
        // DryWetMidi's TimeSignatureEvent handles the log2 conversion internally
        byte numerator = (byte)metadata.TimeSignature.Numerator;
        byte denominator = (byte)metadata.TimeSignature.Denominator;
        events.Add(new TimeSignatureEvent(numerator, denominator));

        // Add key signature event
        // KeySignature.Sharps is positive for sharps, negative for flats
        // Scale: 0 = major, 1 = minor (we always use 0 since we don't track major/minor)
        events.Add(new KeySignatureEvent((sbyte)metadata.KeySignature.Sharps, 0));
    }

    private sealed record TimedEvent(INotationEvent Event, Rational AbsoluteTime);

    private static List<TimedEvent> FlattenMeasures(IReadOnlyList<Measure> measures)
    {
        var timedEvents = new List<TimedEvent>();
        var currentTime = Rational.Zero;

        foreach (var measure in measures)
        {
            foreach (var evt in measure.Events)
            {
                timedEvents.Add(new TimedEvent(evt, currentTime));
                currentTime += evt.Duration.ToBeats();
            }
        }

        return timedEvents;
    }

    private static List<MidiNote> ProcessEvents(List<TimedEvent> timedEvents, MidiExportOptions options)
    {
        var midiNotes = new List<MidiNote>();

        for (int i = 0; i < timedEvents.Count; i++)
        {
            var timedEvent = timedEvents[i];

            switch (timedEvent.Event)
            {
                case NotationNote note:
                    ProcessNote(note, timedEvent.AbsoluteTime, timedEvents, ref i, midiNotes, options);
                    break;

                case Notation.Chord chord:
                    ProcessChord(chord, timedEvent.AbsoluteTime, timedEvents, ref i, midiNotes, options);
                    break;

                case Notation.Rest:
                    // Rests don't produce MIDI notes, just advance time
                    break;
            }
        }

        return midiNotes;
    }

    private static void ProcessNote(
        NotationNote note,
        Rational startTime,
        List<TimedEvent> timedEvents,
        ref int currentIndex,
        List<MidiNote> midiNotes,
        MidiExportOptions options)
    {
        var totalDuration = note.Duration.ToBeats();

        // If this note starts a tie, accumulate duration from following tied notes
        if (note.Tie == TieType.Start)
        {
            // Look ahead for tied notes
            for (int j = currentIndex + 1; j < timedEvents.Count; j++)
            {
                if (timedEvents[j].Event is NotationNote nextNote &&
                    nextNote.Pitch.Equals(note.Pitch))
                {
                    totalDuration += nextNote.Duration.ToBeats();

                    // If this note ends the tie, stop
                    if (nextNote.Tie == TieType.None || nextNote.Tie == TieType.End)
                    {
                        currentIndex = j; // Skip the tied notes we've accumulated
                        break;
                    }
                }
                else
                {
                    // Different note or event type, stop looking
                    break;
                }
            }
        }

        // Convert to MIDI note
        var midiNoteNumber = PitchToMidiNote(note.Pitch);
        var startTicks = BeatsToTicks(startTime, options.TicksPerQuarterNote);
        var lengthTicks = BeatsToTicks(totalDuration, options.TicksPerQuarterNote);

        midiNotes.Add(new MidiNote(
            (SevenBitNumber)midiNoteNumber,
            lengthTicks,
            startTicks)
        {
            Channel = (FourBitNumber)0,
            Velocity = (SevenBitNumber)note.Velocity.MidiVelocity
        });
    }

    private static void ProcessChord(
        Notation.Chord chord,
        Rational startTime,
        List<TimedEvent> timedEvents,
        ref int currentIndex,
        List<MidiNote> midiNotes,
        MidiExportOptions options)
    {
        var totalDuration = chord.Duration.ToBeats();

        // If this chord starts a tie, accumulate duration
        if (chord.Tie == TieType.Start)
        {
            for (int j = currentIndex + 1; j < timedEvents.Count; j++)
            {
                if (timedEvents[j].Event is Notation.Chord nextChord &&
                    nextChord.Equals(chord))
                {
                    totalDuration += nextChord.Duration.ToBeats();

                    if (nextChord.Tie == TieType.None || nextChord.Tie == TieType.End)
                    {
                        currentIndex = j;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        // Convert each pitch in the chord to a MIDI note
        var startTicks = BeatsToTicks(startTime, options.TicksPerQuarterNote);
        var lengthTicks = BeatsToTicks(totalDuration, options.TicksPerQuarterNote);

        foreach (var pitch in chord.Pitches)
        {
            var midiNoteNumber = PitchToMidiNote(pitch);

            midiNotes.Add(new MidiNote(
                (SevenBitNumber)midiNoteNumber,
                lengthTicks,
                startTicks)
            {
                Channel = (FourBitNumber)0,
                Velocity = (SevenBitNumber)chord.Velocity.MidiVelocity
            });
        }
    }

    private static int PitchToMidiNote(Pitch pitch)
    {
        // Base MIDI note number from pitch class (C = 0, C# = 1, D = 2, etc.)
        int baseNote = pitch.PitchClass switch
        {
            PitchClass.C => 0,
            PitchClass.CSharp => 1,
            PitchClass.D => 2,
            PitchClass.DSharp => 3,
            PitchClass.E => 4,
            PitchClass.F => 5,
            PitchClass.FSharp => 6,
            PitchClass.G => 7,
            PitchClass.GSharp => 8,
            PitchClass.A => 9,
            PitchClass.ASharp => 10,
            PitchClass.B => 11,
            _ => 0
        };

        // MIDI octave offset: C4 (middle C) = 60, so octave 4 starts at 48 (C4 = 48 + 12 = 60)
        int midiNote = (pitch.Octave + 1) * 12 + baseNote;

        // Apply accidental
        if (pitch.Accidental.HasValue)
        {
            midiNote += pitch.Accidental.Value switch
            {
                Accidental.DoubleFlat => -2,
                Accidental.Flat => -1,
                Accidental.Natural => 0,
                Accidental.Sharp => 1,
                Accidental.DoubleSharp => 2,
                Accidental.QuarterFlat => 0, // Round to nearest semitone
                Accidental.QuarterSharp => 0,
                Accidental.ThreeQuarterFlat => -1,
                Accidental.ThreeQuarterSharp => 1,
                _ => 0
            };
        }

        return Math.Clamp(midiNote, 0, 127);
    }

    private static long BeatsToTicks(Rational beats, int ticksPerQuarterNote)
    {
        // Quarter note = 1 beat
        // ticks = beats * ticksPerQuarterNote
        var ticks = beats.ToDouble() * ticksPerQuarterNote;
        return (long)Math.Round(ticks);
    }
}
