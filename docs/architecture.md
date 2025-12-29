# StaffSharp Architecture

A music notation system using dual intermediate representations (IR) for clean separation between performance and notation.

**Quick Links:**
- [Pipeline Flows](architecture/pipeline-flows.md) - Data flow diagrams
- [Implementation Notes](architecture/implementation-notes.md) - Patterns and gotchas

---

## Why Two IRs?

**Problem:** Music has two representations:
1. **Performance** - "what plays when" (timing, pitch, dynamics)
2. **Notation** - "how it's written" (measures, ties, beaming)

**Solution:** Use separate IRs optimized for each purpose:

```
IR1: Performance Timeline          IR2: Notation Score
• Flat event list                  • Hierarchical (parts/voices/measures)
• Rational time (exact beats)      • Symbolic durations (quarter, eighth, etc.)
• Format-agnostic                  • Notation artifacts (ties, beaming)
• Easy queries                     • Rendering-ready
• Canonical for audio sources      • Required for all outputs
```

**Data Flow:**
```
Audio → IR1 → IR2 → [SVG, ABC, MIDI, WAV]
ABC → IR2 → [SVG, ABC, MIDI, WAV]
```

**Benefits:**
- All outputs from IR2 (consistent)
- IR1 only for performance sources (audio)
- ABC parses directly to IR2 (natural)
- Multiple notation styles from same performance
- No need for IR2 → IR1 conversion

---

## IR1: Performance Timeline

### Purpose
The canonical "source of truth" for musical content. Represents timing and pitch without notation decisions.

### Key Types

```csharp
// Top-level container
class PerformanceTimeline {
    TempoMap TempoMap
    IReadOnlyList<IPerformanceEvent> Events  // Sorted by onset
    PerformanceMetadata Metadata
}

// All events implement this
interface IPerformanceEvent {
    Rational OnsetBeats  // From start of piece
}

// Exact fractional time (no float errors)
record struct Rational(int Numerator, int Denominator) {
    Rational Simplify()
    double ToDouble()
    // +, -, *, / operators
}

// From audio analysis (real time)
record NoteEvent(
    MidiNote Pitch,
    TimeSpan Onset,      // Real time
    TimeSpan Duration,
    Velocity Velocity,
    int? VoiceHint,      // Suggested voice (1, 2, 3...)
)

// Wrapper: adds musical time to audio events
record QuantizedNoteEvent(
    NoteEvent RawEvent,           // Preserves original
    Rational OnsetBeats,          // Musical time
    Rational DurationBeats,
    QuantizationMetadata Metadata,
    ArticulationFlags Articulation // Staccato, accent, etc.
) : IPerformanceEvent

// From MIDI/ABC/MusicXML (already symbolic)
record SymbolicNoteEvent(
    MidiNote Pitch,
    Rational OnsetBeats,
    Rational DurationBeats,
    Velocity Velocity,
    int? VoiceHint,
    ArticulationFlags Articulation
) : IPerformanceEvent

// Tempo and time signature mapping
class TempoMap {
    IReadOnlyList<TempoChange> TempoChanges
    IReadOnlyList<TimeSignatureChange> TimeSignatures

    double BeatsToSeconds(Rational beats)
    Rational SecondsToBeats(double seconds)
    MeasureLocation GetMeasureAt(Rational beats)  // Which measure and beat within it
}

// Supporting types for IR1
record PerformanceMetadata(
    string? Title,
    string? Composer,
    string? Copyright
)

record QuantizationMetadata(
    int Subdivision,              // 16 = sixteenth notes
    double TempoAtOnset,          // BPM when note started
    TimeSpan QuantizationError    // How much onset was shifted
)

[Flags]
enum ArticulationFlags {
    None = 0,
    Staccato = 1,    // Short, detached
    Accent = 2,      // Emphasized
    Tenuto = 4,      // Held full value
    Marcato = 8,     // Strongly accented
    Fermata = 16     // Hold longer than written
}

record struct MeasureLocation(
    int MeasureNumber,            // 1-indexed
    Rational BeatInMeasure        // Position within measure (0-based)
)
```

### Design Decisions

**Rational vs Float:**
- ✅ Exact: 1/3, dotted notes, tuplets
- ✅ No accumulation errors
- ✅ Can detect tuplets from values

**Flat vs Hierarchical:**
- ✅ Easy queries: "what's at beat 5.25?"
- ✅ No arbitrary measure decisions
- ✅ Trivial MIDI export

**Wrapper Pattern:**
- QuantizedNoteEvent wraps NoteEvent to preserve original audio timing
- Can re-quantize with different parameters without data loss

---

## IR2: Notation Score

### Purpose
Derived representation for rendering music notation. Contains presentation logic (ties, beaming, measure breaks).

### Key Types

```csharp
// Top-level container
class NotationScore {
    ScoreMetadata Metadata  // Title, composer, key, time sig
    IReadOnlyList<Part> Parts

    // Derived index for queries (rebuilt when score changes)
    IEnumerable<INotationEvent> EventsAt(Rational beat)
}

record ScoreMetadata(
    string? Title,
    string? Composer,
    KeySignature KeySignature,    // C, G, D, ... (sharps/flats)
    TimeSignature DefaultTimeSignature,
    int DefaultTempo              // BPM
)

// Hierarchy: Score → Parts → Voices → Measures → Events
class Part {
    PartInfo Info              // Name, clef, transpose
    IReadOnlyList<Voice> Voices
}

class Voice {
    int VoiceNumber
    IReadOnlyList<Measure> Measures
}

class Measure {
    int Number
    TimeSignature? TimeSignature  // Only if changed
    IReadOnlyList<INotationEvent> Events

    // Events must sum to measure duration
}

// All notation events have symbolic duration
interface INotationEvent {
    SymbolicDuration Duration
}

record NotationNote(
    Pitch Pitch,
    SymbolicDuration Duration,
    float Velocity,
    TieType Tie,                  // None/Start/End/Both
    Accidental? Accidental,
    ArticulationFlags Articulation
) : INotationEvent

record Rest(SymbolicDuration Duration) : INotationEvent

// Symbolic duration with dots and tuplets
record SymbolicDuration(
    NoteDurationBase Base,     // Whole=1, Half=2, Quarter=4, etc.
    int Dots = 0,              // 0, 1 (dotted), 2 (double-dotted)
    Tuplet? Tuplet = null
) {
    Rational ToBeats()  // Convert to beats for validation
}

record Tuplet(int ActualNotes, int NormalNotes)  // (3,2) = triplet

enum NoteDurationBase { Whole=1, Half=2, Quarter=4, Eighth=8, ... }
enum TieType { None, Start, End, Both }
```

### Design Decisions

**Ties in IR2 Only:**
- IR1: Note is just onset + duration
- IR2: Ties added when duration doesn't fit in measure
- NotationEngine decides where to add ties

**Voice-per-Stream:**
- ✅ Natural for sequential rendering (left to right)
- ✅ Each voice maintains own timeline
- ✅ Easy to validate measure boundaries
- ❌ Queries need derived index (rebuild when score changes)

---

## Conversion Algorithms

### IR1 → IR2: NotationEngine

Converts flat performance timeline to hierarchical notation.

```
Input: PerformanceTimeline (IR1)
Output: NotationScore (IR2)

Algorithm:
1. Voice Assignment
   - Group overlapping notes into separate voices
   - Use VoiceHint if available
   - Otherwise: analyze pitch ranges and overlaps

2. Measure Breaking
   - Calculate measure boundaries from TempoMap
   - Split notes that span barlines (add ties)

3. Rational → Symbolic Duration
   - Try exact matches (1/4 → Quarter)
   - Try dotted notes (3/2 → DottedQuarter)
   - Try tuplets (1/3 → TripletEighth)
   - Fall back to approximation

4. Tie Insertion
   - Ties already added in measure breaking
   - Validate tie chains

5. Tuplet Detection
   - Find consecutive notes with same tuplet ratio
   - Group for rendering
```

**Challenges:**
- Voice assignment is constraint satisfaction (NP-hard in general)
- Duration conversion has many valid choices (use NotationRules for preferences)
- Ties vs dots: configurable preference

### IR2 → Outputs: Export Pipeline

All outputs generated from IR2:

**MIDI Export from IR2:**
```
Algorithm:
1. Walk each voice in each part
2. Track current beat position
3. Convert NotationNote → MIDI note on/off
4. Merge tied notes into single duration
5. Convert Rational beats → MIDI ticks
6. Add tempo/time signature events
```

**ABC/MusicXML Export:**
```
Direct emission from IR2 structure
(Parts → Voices → Measures → Events)
```

**SVG Rendering:**
```
Layout engine processes IR2 hierarchically
Renders glyphs for each measure
```

**WAV Synthesis (future):**
```
Walk IR2, synthesize audio for each note
```

---

## Trade-offs

### Audio → IR1: Quantization Loss
- **Challenge:** Converting real time to musical time requires tempo detection and quantization
- **Loss:** Microtiming, groove, expressive timing
- **Mitigation:** Wrapper pattern preserves original TimeSpan data

### IR1 → IR2: Notation Ambiguity
- **Challenge:** Duration 5/8 can be notated many ways (half + eighth, dotted quarter + sixteenth + eighth, etc.)
- **Decision:** Somewhat arbitrary, needs NotationRules engine
- **Mitigation:** Configurable preferences (prefer ties vs dots, max dots, allow tuplets)

### ABC Round-Trip: Notation Preservation
- **Challenge:** ABC → IR2 → ABC might lose subtle notation choices
- **Mitigation:** Cache original IR2 on import, reuse on export for lossless round-trip

---

## Key Architectural Decisions

### 1. Two IRs Instead of One

**Why not single unified IR?**
- Would force compromise between playback precision and notation flexibility
- Professional software (MuseScore, Finale, Sibelius) all use dual-IR internally
- Validates our approach

### 2. Rational Time Instead of Float

**Why not doubles?**
- Floating point accumulation errors
- Can't represent 1/3 exactly
- Difficult to detect tuplets from quantized values
- Need to simplify fractions to keep denominators bounded

### 3. Flat IR1 Instead of Hierarchical

**Why not hierarchical primary IR?**
- Queries harder: "what plays at beat X"
- Forces early measure break decisions
- Mixing performance and notation concerns
- Better to separate performance (IR1) from notation (IR2)

### 4. Voice Hints Instead of Required Voices

**Why optional in IR1?**
- Audio analysis may not detect polyphony correctly
- NotationEngine can reassign for better engraving
- Symbolic sources (MIDI with channels) can provide strong hints

### 5. Supporting Metadata Types

**Why include them?**
- PerformanceMetadata: Track title/composer through pipeline
- QuantizationMetadata: Debug quantization issues, compare original vs quantized
- ArticulationFlags: Performance information that affects both playback and notation
- MeasureLocation: Fast "which measure am I in?" queries

---

## Comparison with Other Systems

**MuseScore/Finale/Sibelius:**
- Similar dual-IR approach (performance + notation)
- Validates our architecture

**Music21 (Python):**
- Separates `stream` (sequential) from `score` (hierarchical)
- Similar philosophy to IR1/IR2

**Lilypond:**
- Separates music expression from engraving rules
- Input language vs output formatting

**MIDI Specification:**
- Tick-based timing (like our Rational beats)
- Event-based flat timeline (like IR1)
- Note: We export MIDI from IR2 (after notation generation) for consistency

---

## Project Organization

```
src/
├── StaffSharp.Core/           # IR1 and IR2 types, primitives
├── StaffSharp.Audio/          # WAV → IR1 pipeline, core audio processing
├── StaffSharp.Conversion/     # IR1 → IR2 (NotationEngine)
├── StaffSharp.Importers/      # ABC → IR2 parser
├── StaffSharp.Exporters/      # IR2 → SVG, ABC, WAV (Dependency-free exporters)
├── StaffSharp.Exporters.Midi/ # IR2 → MIDI (Requires external dependency)
└── StaffSharp.Cli/            # Command-line interface

test/
├── StaffSharp.Core.Tests/
├── StaffSharp.Audio.Tests/
└── ...
```

**Dependency Flow:**
```
Core (no dependencies)
  ↑
  ├── Audio (produces IR1)
  ├── Conversion (IR1 → IR2)
  ├── Importers (produces IR2)
  └── Exporters (consumes IR2)

Cli (depends on all above)
```

---

## Next Steps

1. **Implement Core Types** (StaffSharp.Core)
   - Rational, Frequency, MidiNote, Pitch
   - PerformanceTimeline, IPerformanceEvent
   - NotationScore, INotationEvent
   - Write comprehensive tests

2. **Build Audio Pipeline** (StaffSharp.Audio)
   - Start with simple monophonic audio
   - Add diagnostics from day one
   - Test each stage independently

3. **Implement Conversion** (StaffSharp.Conversion)
   - NotationEngine: IR1 → IR2
   - Test with simple monophonic melodies first

4. **Add Importers/Exporters**
   - ABC parser → IR2
   - IR2 → ABC emitter (test round-trip)
   - IR2 → MIDI emitter
   - IR2 → SVG renderer

5. **Build CLI**
   - `convert` command
   - `diagnose` command with full pipeline diagnostics

See [Pipeline Flows](architecture/pipeline-flows.md) for detailed data flow diagrams.
See [Implementation Notes](architecture/implementation-notes.md) for coding patterns and gotchas.
