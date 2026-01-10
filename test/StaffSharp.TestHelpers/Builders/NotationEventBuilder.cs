namespace StaffSharp.TestHelpers.Builders;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Fluent builder for creating notation events in tests.
/// </summary>
public sealed class NotationEventBuilder
{
    private readonly List<INotationEvent> _events = new();
    private int _defaultOctave = 4;
    private SymbolicDuration _defaultDuration = SymbolicDuration.Quarter;
    private Velocity _defaultVelocity = Velocity.MezzoForte;

    /// <summary>
    /// Creates a new notation event builder.
    /// </summary>
    public static NotationEventBuilder Create() => new();

    /// <summary>
    /// Sets the default octave for subsequent notes.
    /// </summary>
    public NotationEventBuilder DefaultOctave(int octave)
    {
        _defaultOctave = octave;
        return this;
    }

    /// <summary>
    /// Sets the default duration for subsequent events.
    /// </summary>
    public NotationEventBuilder DefaultDuration(SymbolicDuration duration)
    {
        _defaultDuration = duration;
        return this;
    }

    /// <summary>
    /// Sets the default velocity for subsequent notes.
    /// </summary>
    public NotationEventBuilder DefaultVelocity(Velocity velocity)
    {
        _defaultVelocity = velocity;
        return this;
    }

    // Note methods for each pitch class
    public NotationEventBuilder C(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.C, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder CSharp(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.CSharp, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder D(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.D, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder DSharp(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.DSharp, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder E(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.E, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder F(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.F, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder FSharp(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.FSharp, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder G(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.G, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder GSharp(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.GSharp, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder A(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.A, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder ASharp(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.ASharp, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    public NotationEventBuilder B(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, int slurNumber = 1, Accidental? accidental = null, IReadOnlyList<Decoration>? decorations = null)
        => AddNote(PitchClass.B, octave, duration, velocity, tieMarker, slurMarker, slurNumber, accidental, decorations);

    /// <summary>
    /// Adds a rest.
    /// </summary>
    public NotationEventBuilder Rest(SymbolicDuration? duration = null)
    {
        _events.Add(new Notation.Rest(duration ?? _defaultDuration));
        return this;
    }

    /// <summary>
    /// Adds a chord with the specified pitch classes at the default octave.
    /// </summary>
    public NotationEventBuilder Chord(params PitchClass[] pitchClasses)
        => Chord(null, null, null, null, null, null, 1, pitchClasses);

    /// <summary>
    /// Adds a chord with full control over parameters.
    /// </summary>
    public NotationEventBuilder Chord(int? octave = null, SymbolicDuration? duration = null, Velocity? velocity = null, TieMarkerType? tieMarker = null, SlurMarkerType? slurMarker = null, Decoration[]? decorations = null, int slurNumber = 1, params PitchClass[] pitchClasses)
    {
        var pitches = pitchClasses.Select(pc => new Pitch(pc, octave ?? _defaultOctave)).ToList();

        var slurMarkers = slurMarker == null
            ? null
            : new[] { new SlurMarker(slurNumber, slurMarker.GetValueOrDefault()) };

        _events.Add(new Chord(
            pitches,
            duration ?? _defaultDuration,
            velocity ?? _defaultVelocity,
            tieMarker == null ? null : new TieMarker(tieMarker.GetValueOrDefault()),
            graceNote: null,
            decorations: decorations,
            chordSymbol: null,
            annotation: null,
            slurMarkers: slurMarkers));
        return this;
    }

    /// <summary>
    /// Builds the list of notation events.
    /// </summary>
    public IReadOnlyList<INotationEvent> Build() => _events;

    private NotationEventBuilder AddNote(
        PitchClass pitchClass,
        int? octave,
        SymbolicDuration? duration,
        Velocity? velocity,
        TieMarkerType? tieMarkerType,
        SlurMarkerType? slurMarkerType,
        int slurNumber,
        Accidental? accidental,
        IReadOnlyList<Decoration>? decorations)
    {
        var slurMarkers = slurMarkerType == null
            ? null
            : new[] { new SlurMarker(slurNumber, slurMarkerType.GetValueOrDefault()) };

        _events.Add(
            new NotationNote(
                new Pitch(pitchClass, octave ?? _defaultOctave, accidental),
                duration ?? _defaultDuration,
                velocity ?? _defaultVelocity,
                tieMarkerType == null ? null : new TieMarker(tieMarkerType.GetValueOrDefault()),
                GraceNote: null,
                Decorations: decorations,
                ChordSymbol: null,
                Annotation: null,
                SlurMarkers: slurMarkers));

        return this;
    }
}
