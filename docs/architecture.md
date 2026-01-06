# StaffSharp Architecture

StaffSharp uses two intermediate representations to separate performance data (what plays when) from notation decisions (how it's written).

## The Two IRs

Music needs two different representations:

**Performance (IR1)** - Flat list of note events with exact timing. This is what you get from audio analysis or MIDI recording. Everything has a concrete onset time and duration in beats.

**Notation (IR2)** - Hierarchical score structure with measures, voices, and symbolic durations (quarter notes, dotted eighths, etc.). This is what you need for rendering sheet music or exporting to notation formats.

The split matters because:
- Audio sources produce timing data, not notation
- Notation formats (ABC, MusicXML) already think in measures and symbolic durations
- All outputs need IR2 anyway (MIDI, SVG, ABC exports all work from notation structure)
- Converting real timing to notation involves subjective decisions (where to add ties, how to beam notes)

Flow:
```
Audio → IR1 → IR2 → [MIDI, SVG, ABC, ...]
MusicXML/ABC → IR2 → [MIDI, SVG, ...]
```

## IR1: Performance Timeline

The flat event list with rational timing. This is the source of truth for musical content from audio or real-time performance.

**Key types:**

```csharp
// Container
class PerformanceTimeline {
    TempoMap TempoMap
    IReadOnlyList<IPerformanceEvent> Events  // Sorted by onset
    PerformanceMetadata Metadata
}

// All events have an onset time in beats
interface IPerformanceEvent {
    Rational OnsetBeats
}

// Exact fractional time - no floating point errors
record struct Rational(int Numerator, int Denominator)

// From audio analysis
record NoteEvent(
    MidiNote Pitch,
    TimeSpan Onset,      // Real time from audio
    TimeSpan Duration,
    Velocity Velocity,
    int? VoiceHint
)

// After quantization (wraps NoteEvent to preserve original timing)
record QuantizedNoteEvent(
    NoteEvent RawEvent,
    Rational OnsetBeats,
    Rational DurationBeats,
    QuantizationMetadata Metadata
) : IPerformanceEvent

// From symbolic sources (MIDI, MusicXML, ABC)
record SymbolicNoteEvent(
    MidiNote Pitch,
    Rational OnsetBeats,
    Rational DurationBeats,
    Velocity Velocity,
    int? VoiceHint
) : IPerformanceEvent

// Tempo and time signature
class TempoMap {
    IReadOnlyList<TempoChange> TempoChanges
    IReadOnlyList<TimeSignatureChange> TimeSignatures
    
    double BeatsToSeconds(Rational beats)
    Rational SecondsToBeats(double seconds)
    MeasureLocation GetMeasureAt(Rational beats)
}
```

**Why rational numbers?**

Rational arithmetic is exact. `1/3` stays `1/3`, no accumulation errors. You can detect tuplets from the denominators. The tradeoff is you need to simplify fractions periodically to keep denominators bounded.

**Why flat instead of hierarchical?**

Easy queries - "what's playing at beat 5.25?" is trivial. No arbitrary measure break decisions. MIDI export is straightforward. The downside is you need TempoMap to figure out measure boundaries.

## IR2: Notation Score

The hierarchical notation structure. This is what gets rendered as sheet music or exported to notation formats.

**Key types:**

```csharp
// Container
class NotationScore {
    ScoreMetadata Metadata
    IReadOnlyList<Part> Parts
}

// Hierarchy: Score → Parts → Staves → Voices → Measures → Events
class Part {
    string Name
    IReadOnlyList<Staff> Staves  // Most instruments have 1, piano has 2
}

class Staff {
    int Number       // 1-based, top to bottom
    Clef Clef
    IReadOnlyList<Voice> Voices
}

class Voice {
    int Number
    IReadOnlyList<Measure> Measures
}

class Measure {
    int Number
    TimeSignature? TimeSignature  // Only if changed from previous
    IReadOnlyList<INotationEvent> Events
}

// Events have symbolic duration
interface INotationEvent {
    SymbolicDuration Duration
}

record NotationNote(
    Pitch Pitch,
    SymbolicDuration Duration,
    float Velocity,
    TieType Tie,              // None/Start/End/Both
    Accidental? Accidental,
    ArticulationFlags Articulation
) : INotationEvent

record Rest(SymbolicDuration Duration) : INotationEvent

// Duration represented symbolically
record SymbolicDuration(
    NoteDurationBase Base,    // Whole=1, Half=2, Quarter=4, Eighth=8...
    int Dots = 0,
    Tuplet? Tuplet = null
) {
    Rational ToBeats()        // For validation
}

record Tuplet(int ActualNotes, int NormalNotes)  // (3,2) = triplet

enum TieType { None, Start, End, Both }
```

**Why ties only in IR2?**

In IR1, a note is just onset + duration. In IR2, when a note doesn't fit in a measure, the NotationEngine splits it and adds ties. This keeps notation decisions out of the performance representation.

**Why voice-per-stream?**

Each voice is a sequence of measures, rendered left to right. Makes validation easy (measure durations must add up). The downside is querying "what's at beat X" needs a derived index.

## Converting IR1 to IR2

The NotationEngine converts the flat performance timeline into hierarchical notation. Lives in StaffSharp.Core.

**Steps:**

1. **Voice assignment** - Group overlapping notes into separate voices. Uses VoiceHint from audio analysis if available, otherwise analyzes pitch ranges and overlaps. This is basically constraint satisfaction and gets complicated fast.

2. **Staff splitting** - Decide if we need a grand staff (piano: treble + bass) or single staff. Looks at pitch ranges.

3. **Measure partitioning** - Calculate measure boundaries from TempoMap. Notes that span barlines get split with ties added.

4. **Duration conversion** - Convert rational beats to symbolic durations. Try exact matches first (1/4 → Quarter), then dotted notes (3/8 → DottedEighth), then tuplets. There are usually multiple valid choices here.

5. **Validation** - Make sure each measure's events sum to the correct duration.

**The hard parts:**

- Voice assignment can have many valid solutions. Which is "best" for readability?
- Duration conversion is ambiguous. Same rational can be written multiple ways (ties vs dots, how to beam).
- Tuplet detection needs to find consecutive notes with matching ratios.

Currently using simple heuristics. A proper solution would use configurable notation preferences.

## Exports

All exports work from IR2. This keeps output consistent regardless of source format.

**MIDI:** Walk the notation hierarchy (parts → staves → voices → measures → events), convert NotationNote events to MIDI note on/off messages. Merge tied notes into single durations. Convert rational beats to MIDI ticks using tempo map.

**SVG:** Layout engine processes IR2 hierarchically, renders glyphs for each measure. Very much work in progress.

**ABC/MusicXML:** Direct emission from IR2 structure. Parts map to parts, voices to voices, etc.

The tricky bit is tied notes - need to merge them back for MIDI/audio output, but preserve them for notation rendering.

## Tradeoffs

**Audio → IR1: Lost timing nuances**

Converting real time to musical time requires tempo detection and quantization. You lose microtiming, groove, expressive rubato. The QuantizedNoteEvent wrapper preserves the original TimeSpan data so you can re-quantize with different parameters, but the original performance feel is gone.

**IR1 → IR2: Notation ambiguity**

Duration `5/8` beats can be written many ways - half + eighth, dotted quarter + sixteenth + eighth, etc. The NotationEngine makes somewhat arbitrary choices. Should probably have configurable preferences for ties vs dots, max dots allowed, tuplet usage.

**Round-trip loss**

ABC → IR2 → ABC might not preserve subtle notation choices. Could cache the original IR2 on import and reuse it for lossless round-trips, but that's not implemented yet.

## Project structure

```
src/
├── StaffSharp.Core/            # IR1/IR2 types, NotationEngine, primitives
├── StaffSharp.Audio/           # DSP pipeline (pitch/onset detection)
├── StaffSharp.MusicXml/        # MusicXML → IR2
├── StaffSharp.Importers/       # ABC → IR2 💭 I think this project should be StaffSharp.Abc now!
├── StaffSharp.Midi/            # IR2 → MIDI
├── StaffSharp.Svg/             # IR2 → SVG (WIP)
└── StaffSharp.Cli/             # Command-line tool

test/
├── StaffSharp.Core.Tests/
├── StaffSharp.Audio.Tests/
└── ...

tools/
├── StaffSharp.Demo/            # A very much WIP demo UI
└── StaffSharp.SvgRenderDebug/  # A debug UI that can break out early of the render pipeline
```

**Dependencies:**

Core has no dependencies. Everything else depends on Core. Audio produces IR1, importers produce IR2, NotationEngine converts IR1 → IR2, exporters consume IR2. Cli ties it all together.

Originally planned separate Conversion and Exporters projects, but NotationEngine ended up in Core (keeps the conversion logic close to the types), and each exporter is separate (MIDI, Svg, etc.) to manage dependencies.
