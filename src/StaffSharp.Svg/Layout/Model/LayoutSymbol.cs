namespace StaffSharp.Layout.Model;

/// <summary>
/// Base class for positioned musical symbols.
/// </summary>
public abstract class LayoutSymbol : LayoutElement, ILayoutSymbol
{
    /// <summary>
    /// Time position of this symbol in musical time (e.g., quarter note = 1.0).
    /// </summary>
    public double TimePosition { get; set; }

    /// <summary>
    /// Voice number this symbol belongs to (1-based). 0 for non-voice elements like clefs/barlines.
    /// </summary>
    public int VoiceNumber { get; set; }

    /// <summary>
    /// Spacing (padding) around this symbol, calculated during MeasureWidthCalculationPass.
    /// Left and Right padding are used by HorizontalPositionPass for positioning.
    /// </summary>
    public LayoutSpacing Spacing { get; set; }

    public int LedgerLineCount { get; set; }
    public bool LedgerLinesAbove { get; set; }
}
