namespace StaffSharp.Svg.Layout;

using StaffSharp.Notation;

/// <summary>
/// Base class for positioned musical symbols.
/// </summary>
public abstract class LayoutSymbol : LayoutElement
{
    /// <summary>
    /// Time position of this symbol in musical time (e.g., quarter note = 1.0).
    /// </summary>
    public double TimePosition { get; set; }

    /// <summary>
    /// Voice number this symbol belongs to (1-based). 0 for non-voice elements like clefs/barlines.
    /// </summary>
    public int VoiceNumber { get; set; }

    // Stem and beam information (for Phase 2)
    public double StemX { get; set; }
    public double StemY1 { get; set; }
    public double StemY2 { get; set; }
    public bool StemUp { get; set; }

    // Beam group information
    public int? BeamGroupId { get; set; }
    public bool IsFirstInBeamGroup { get; set; }
    public bool IsLastInBeamGroup { get; set; }
    public int BeamCount { get; set; } // Number of beams (1 for eighth, 2 for sixteenth, etc.)

    public int LedgerLineCount { get; set; }
    public bool LedgerLinesAbove { get; set; }

    // Accidental information (for Phase 2)
    public Accidental? Accidental { get; set; }
    public double AccidentalX { get; set; }
    public double AccidentalY { get; set; }
}

/// <summary>
/// Represents a positioned note.
/// </summary>
public sealed class NoteLayoutSymbol : LayoutSymbol
{
    public required NotationNote Note { get; init; }
}

/// <summary>
/// Represents a positioned rest.
/// </summary>
public sealed class RestLayoutSymbol : LayoutSymbol
{
    public required Rest Rest { get; init; }
}

/// <summary>
/// Represents a positioned chord.
/// </summary>
public sealed class ChordLayoutSymbol : LayoutSymbol
{
    public required Chord Chord { get; init; }
    public IList<double> NoteheadYPositions { get; } = [];
    public IList<double> NoteheadXShifts { get; } = [];
    public IList<bool> AccidentalShifts { get; } = [];
    public IList<Accidental> Accidentals { get; } = [];
    public IList<double> AccidentalXOffsets { get; } = [];
    public IList<double> AccidentalYPositions { get; } = [];
}

/// <summary>
/// Represents a positioned clef.
/// </summary>
public sealed class ClefLayoutSymbol : LayoutSymbol
{
    public required Clef Clef { get; init; }
}

/// <summary>
/// Represents a positioned key signature.
/// </summary>
public sealed class KeySignatureLayoutSymbol : LayoutSymbol
{
    public required KeySignature KeySignature { get; init; }
    public Clef Clef { get; init; } = Clef.Treble;
}

/// <summary>
/// Represents a positioned time signature.
/// </summary>
public sealed class TimeSignatureLayoutSymbol : LayoutSymbol
{
    public required TimeSignature TimeSignature { get; init; }
}

/// <summary>
/// Represents a positioned barline.
/// </summary>
public sealed class BarlineLayoutSymbol : LayoutSymbol
{
    public required BarlineType BarlineType { get; init; }
}