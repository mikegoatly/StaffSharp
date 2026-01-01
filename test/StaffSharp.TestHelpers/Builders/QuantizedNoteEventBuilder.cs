using StaffSharp;
using StaffSharp.Performance;

namespace StaffSharp.TestHelpers.Builders;

/// <summary>
/// Fluent builder for creating test QuantizedNoteEvent collections.
/// </summary>
public sealed class QuantizedNoteEventBuilder
{
    private readonly List<QuantizedNoteEvent> _events = new();
    private double _currentBeat;
    private double _defaultDurationBeats = 0.25; // Quarter note in beats
    private Velocity _defaultVelocity = Velocity.MezzoForte;
    private bool _autoAdvancePosition = true;
    private double _tempo = 120.0;
    private int _subdivision = 16;

    private QuantizedNoteEventBuilder() { }

    /// <summary>
    /// Creates a new quantized note event builder.
    /// </summary>
    public static QuantizedNoteEventBuilder Create() => new();

    /// <summary>
    /// Sets the default duration for subsequent notes (in beats).
    /// </summary>
    public QuantizedNoteEventBuilder WithDuration(double durationBeats)
    {
        _defaultDurationBeats = durationBeats;
        return this;
    }

    /// <summary>
    /// Sets the default velocity for subsequent notes.
    /// </summary>
    public QuantizedNoteEventBuilder WithVelocity(Velocity velocity)
    {
        _defaultVelocity = velocity;
        return this;
    }

    /// <summary>
    /// Sets the tempo used for time calculations (default: 120 BPM).
    /// </summary>
    public QuantizedNoteEventBuilder WithTempo(double tempo)
    {
        _tempo = tempo;
        return this;
    }

    /// <summary>
    /// Sets the subdivision used for quantization metadata (default: 16).
    /// </summary>
    public QuantizedNoteEventBuilder WithSubdivision(int subdivision)
    {
        _subdivision = subdivision;
        return this;
    }

    /// <summary>
    /// Sets the position for the next note(s) (in beats).
    /// </summary>
    public QuantizedNoteEventBuilder AtBeat(double beat)
    {
        _currentBeat = beat;
        return this;
    }

    /// <summary>
    /// Disables automatic position advancement after each note.
    /// </summary>
    public QuantizedNoteEventBuilder WithManualPositioning()
    {
        _autoAdvancePosition = false;
        return this;
    }

    /// <summary>
    /// Enables automatic position advancement after each note (default behavior).
    /// </summary>
    public QuantizedNoteEventBuilder WithAutoPositioning()
    {
        _autoAdvancePosition = true;
        return this;
    }

    /// <summary>
    /// Adds a single note at the current position with default duration and velocity.
    /// </summary>
    public QuantizedNoteEventBuilder AddNote(MidiNote pitch)
    {
        AddNoteInternal(_currentBeat, pitch, _defaultDurationBeats, _defaultVelocity);
        
        if (_autoAdvancePosition)
        {
            _currentBeat += _defaultDurationBeats;
        }
        
        return this;
    }

    /// <summary>
    /// Adds a single note with specified duration, using default velocity.
    /// </summary>
    public QuantizedNoteEventBuilder AddNote(MidiNote pitch, double durationBeats)
    {
        AddNoteInternal(_currentBeat, pitch, durationBeats, _defaultVelocity);
        
        if (_autoAdvancePosition)
        {
            _currentBeat += durationBeats;
        }
        
        return this;
    }

    /// <summary>
    /// Adds a single note with specified duration and velocity.
    /// </summary>
    public QuantizedNoteEventBuilder AddNote(MidiNote pitch, double durationBeats, Velocity velocity)
    {
        AddNoteInternal(_currentBeat, pitch, durationBeats, velocity);
        
        if (_autoAdvancePosition)
        {
            _currentBeat += durationBeats;
        }
        
        return this;
    }

    /// <summary>
    /// Adds a note at a specific position (ignores current position and auto-advance).
    /// </summary>
    public QuantizedNoteEventBuilder AddNoteAt(double beat, MidiNote pitch)
    {
        AddNoteInternal(beat, pitch, _defaultDurationBeats, _defaultVelocity);
        return this;
    }

    /// <summary>
    /// Adds a note at a specific position with custom duration.
    /// </summary>
    public QuantizedNoteEventBuilder AddNoteAt(double beat, MidiNote pitch, double durationBeats)
    {
        AddNoteInternal(beat, pitch, durationBeats, _defaultVelocity);
        return this;
    }

    /// <summary>
    /// Adds a note at a specific position with custom duration and velocity.
    /// </summary>
    public QuantizedNoteEventBuilder AddNoteAt(double beat, MidiNote pitch, double durationBeats, Velocity velocity)
    {
        AddNoteInternal(beat, pitch, durationBeats, velocity);
        return this;
    }

    /// <summary>
    /// Adds multiple notes at the same position (chord) with default duration and velocity.
    /// </summary>
    public QuantizedNoteEventBuilder AddChord(params MidiNote[] pitches)
    {
        foreach (var pitch in pitches)
        {
            AddNoteInternal(_currentBeat, pitch, _defaultDurationBeats, _defaultVelocity);
        }
        
        if (_autoAdvancePosition)
        {
            _currentBeat += _defaultDurationBeats;
        }
        
        return this;
    }

    /// <summary>
    /// Adds multiple notes at the same position (chord) with custom duration.
    /// </summary>
    public QuantizedNoteEventBuilder AddChord(double durationBeats, params MidiNote[] pitches)
    {
        foreach (var pitch in pitches)
        {
            AddNoteInternal(_currentBeat, pitch, durationBeats, _defaultVelocity);
        }
        
        if (_autoAdvancePosition)
        {
            _currentBeat += durationBeats;
        }
        
        return this;
    }

    /// <summary>
    /// Builds the list of quantized note events.
    /// </summary>
    public IReadOnlyList<QuantizedNoteEvent> Build()
    {
        return _events.AsReadOnly();
    }

    private void AddNoteInternal(double onsetBeats, MidiNote pitch, double durationBeats, Velocity velocity)
    {
        // Convert beats to seconds using tempo
        var beatsPerSecond = _tempo / 60.0;
        var onsetSeconds = onsetBeats / beatsPerSecond;
        var durationSeconds = durationBeats / beatsPerSecond;

        var rawEvent = new NoteEvent(
            Pitch: pitch,
            Onset: TimeSpan.FromSeconds(onsetSeconds),
            Duration: TimeSpan.FromSeconds(durationSeconds),
            Velocity: velocity);

        var metadata = new QuantizationMetadata(
            Subdivision: _subdivision,
            TempoAtOnset: _tempo,
            OnsetError: TimeSpan.Zero,
            DurationError: TimeSpan.Zero);

        var quantizedEvent = new QuantizedNoteEvent(
            rawEvent,
            ConvertToRational(onsetBeats),
            ConvertToRational(durationBeats),
            metadata);

        _events.Add(quantizedEvent);
    }

    private static Rational ConvertToRational(double beats)
    {
        // Convert decimal beats to rational (e.g., 0.25 -> 1/4, 0.5 -> 1/2, 1.0 -> 4/4)
        var numerator = (int)(beats * 4.0);
        return Rational.Create(numerator, 4);
    }

    // Convenience static methods for common patterns

    /// <summary>
    /// Creates a single note event (convenience method).
    /// </summary>
    public static IReadOnlyList<QuantizedNoteEvent> SingleNote(MidiNote pitch, double durationBeats = 0.25, Velocity? velocity = null)
    {
        var builder = Create();
        var vel = velocity ?? Velocity.MezzoForte;
        return builder.AddNote(pitch, durationBeats, vel).Build();
    }

    /// <summary>
    /// Creates a sequence of notes with automatic positioning (convenience method).
    /// </summary>
    public static IReadOnlyList<QuantizedNoteEvent> Sequence(params MidiNote[] pitches)
    {
        var builder = Create();
        foreach (var pitch in pitches)
        {
            builder.AddNote(pitch);
        }
        return builder.Build();
    }

    /// <summary>
    /// Creates a chord (convenience method).
    /// </summary>
    public static IReadOnlyList<QuantizedNoteEvent> Chord(params MidiNote[] pitches)
    {
        return Create().AddChord(pitches).Build();
    }
}
