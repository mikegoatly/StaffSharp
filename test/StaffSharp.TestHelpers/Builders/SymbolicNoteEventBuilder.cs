using StaffSharp;
using StaffSharp.Performance;

namespace StaffSharp.TestHelpers.Builders;

/// <summary>
/// Fluent builder for creating test SymbolicNoteEvent collections.
/// </summary>
public sealed class SymbolicNoteEventBuilder
{
    private readonly List<IPerformanceEvent> _events = new();
    private Rational _currentPosition = Rational.Zero;
    private Rational _defaultDuration = Rational.Create(1, 4); // Quarter note
    private Velocity _defaultVelocity = Velocity.MezzoForte;
    private bool _autoAdvancePosition = true;
    private int? _voiceHint;

    private SymbolicNoteEventBuilder() { }

    /// <summary>
    /// Creates a new symbolic note event builder.
    /// </summary>
    public static SymbolicNoteEventBuilder Create() => new();

    public SymbolicNoteEventBuilder WithVoiceHint(int? voiceHint)
    {
        _voiceHint = voiceHint;
        return this;
    }

    /// <summary>
    /// Sets the default duration for subsequent notes.
    /// </summary>
    public SymbolicNoteEventBuilder WithDuration(Rational duration)
    {
        _defaultDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the default duration for subsequent notes (in quarter note units).
    /// </summary>
    public SymbolicNoteEventBuilder WithDuration(int numerator, int denominator)
    {
        _defaultDuration = Rational.Create(numerator, denominator);
        return this;
    }

    /// <summary>
    /// Sets the default velocity for subsequent notes.
    /// </summary>
    public SymbolicNoteEventBuilder WithVelocity(Velocity velocity)
    {
        _defaultVelocity = velocity;
        return this;
    }

    /// <summary>
    /// Sets the position for the next note(s).
    /// </summary>
    public SymbolicNoteEventBuilder AtBeat(Rational position)
    {
        _currentPosition = position;
        return this;
    }

    /// <summary>
    /// Sets the position for the next note(s) (in quarter note units).
    /// </summary>
    public SymbolicNoteEventBuilder AtBeat(int numerator, int denominator)
    {
        _currentPosition = Rational.Create(numerator, denominator);
        return this;
    }

    /// <summary>
    /// Sets the position for the next note(s) as a decimal beat value.
    /// </summary>
    public SymbolicNoteEventBuilder AtBeat(double beat)
    {
        // Convert decimal to rational (e.g., 0.25 -> 1/4, 0.5 -> 1/2)
        var denominator = 4;
        var numerator = (int)(beat * denominator);
        _currentPosition = Rational.Create(numerator, denominator);
        return this;
    }

    /// <summary>
    /// Disables automatic position advancement after each note.
    /// </summary>
    public SymbolicNoteEventBuilder WithManualPositioning()
    {
        _autoAdvancePosition = false;
        return this;
    }

    /// <summary>
    /// Enables automatic position advancement after each note (default behavior).
    /// </summary>
    public SymbolicNoteEventBuilder WithAutoPositioning()
    {
        _autoAdvancePosition = true;
        return this;
    }

    /// <summary>
    /// Adds a single note at the current position with default duration and velocity.
    /// </summary>
    public SymbolicNoteEventBuilder AddNote(MidiNote pitch)
    {
        _events.Add(new SymbolicNoteEvent(pitch, _currentPosition, _defaultDuration, _defaultVelocity, voiceHint: _voiceHint));
        
        if (_autoAdvancePosition)
        {
            _currentPosition += _defaultDuration;
        }
        
        return this;
    }

    /// <summary>
    /// Adds a single note with specified duration, using default velocity.
    /// </summary>
    public SymbolicNoteEventBuilder AddNote(MidiNote pitch, Rational duration)
    {
        _events.Add(new SymbolicNoteEvent(pitch, _currentPosition, duration, _defaultVelocity, voiceHint: _voiceHint));
        
        if (_autoAdvancePosition)
        {
            _currentPosition += duration;
        }
        
        return this;
    }

    /// <summary>
    /// Adds a single note with specified duration and velocity.
    /// </summary>
    public SymbolicNoteEventBuilder AddNote(MidiNote pitch, Rational duration, Velocity velocity)
    {
        _events.Add(new SymbolicNoteEvent(pitch, _currentPosition, duration, velocity, voiceHint: _voiceHint));
        
        if (_autoAdvancePosition)
        {
            _currentPosition += duration;
        }
        
        return this;
    }

    /// <summary>
    /// Adds a note at a specific position (ignores current position and auto-advance).
    /// </summary>
    public SymbolicNoteEventBuilder AddNoteAt(Rational position, MidiNote pitch)
    {
        _events.Add(new SymbolicNoteEvent(pitch, position, _defaultDuration, _defaultVelocity, voiceHint: _voiceHint));
        return this;
    }

    /// <summary>
    /// Adds a note at a specific position with custom duration.
    /// </summary>
    public SymbolicNoteEventBuilder AddNoteAt(Rational position, MidiNote pitch, Rational duration)
    {
        _events.Add(new SymbolicNoteEvent(pitch, position, duration, _defaultVelocity, voiceHint: _voiceHint));
        return this;
    }

    /// <summary>
    /// Adds a note at a specific position with custom duration and velocity.
    /// </summary>
    public SymbolicNoteEventBuilder AddNoteAt(Rational position, MidiNote pitch, Rational duration, Velocity velocity)
    {
        _events.Add(new SymbolicNoteEvent(pitch, position, duration, velocity, voiceHint: _voiceHint));
        return this;
    }

    /// <summary>
    /// Adds multiple notes at the same position (chord) with default duration and velocity.
    /// </summary>
    public SymbolicNoteEventBuilder AddChord(params MidiNote[] pitches)
    {
        foreach (var pitch in pitches)
        {
            _events.Add(new SymbolicNoteEvent(pitch, _currentPosition, _defaultDuration, _defaultVelocity, voiceHint: _voiceHint));
        }
        
        if (_autoAdvancePosition)
        {
            _currentPosition += _defaultDuration;
        }
        
        return this;
    }

    /// <summary>
    /// Adds multiple notes at the same position (chord) with custom duration.
    /// </summary>
    public SymbolicNoteEventBuilder AddChord(Rational duration, params MidiNote[] pitches)
    {
        foreach (var pitch in pitches)
        {
            _events.Add(new SymbolicNoteEvent(pitch, _currentPosition, duration, _defaultVelocity, voiceHint: _voiceHint));
        }
        
        if (_autoAdvancePosition)
        {
            _currentPosition += duration;
        }
        
        return this;
    }

    /// <summary>
    /// Builds the list of performance events.
    /// </summary>
    public IReadOnlyList<IPerformanceEvent> Build()
    {
        return [.. _events];
    }

    // Convenience static methods for common patterns

    /// <summary>
    /// Creates a single note event (convenience method).
    /// </summary>
    public static IReadOnlyList<IPerformanceEvent> SingleNote(MidiNote pitch, Rational? duration = null, Velocity? velocity = null)
    {
        var builder = Create();
        var dur = duration ?? Rational.Create(1, 4);
        var vel = velocity ?? Velocity.MezzoForte;
        return builder.AddNote(pitch, dur, vel).Build();
    }

    /// <summary>
    /// Creates a sequence of notes with automatic positioning (convenience method).
    /// </summary>
    public static IReadOnlyList<IPerformanceEvent> Sequence(params MidiNote[] pitches)
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
    public static IReadOnlyList<IPerformanceEvent> Chord(params MidiNote[] pitches)
    {
        return Create().AddChord(pitches).Build();
    }
}
