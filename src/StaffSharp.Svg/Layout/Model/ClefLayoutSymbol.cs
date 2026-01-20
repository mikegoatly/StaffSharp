namespace StaffSharp.Layout.Model;

using StaffSharp;

using StaffSharp.Layout.Services;
using StaffSharp.Notation;

/// <summary>
/// Represents a positioned clef.
/// </summary>
internal sealed class ClefLayoutSymbol : LayoutSymbol
{
    public required Clef Clef { get; init; }

    internal static LayoutSymbol Create(Clef clef, SvgContext context)
    {
        return new ClefLayoutSymbol
        {
            Clef = clef,
            TimePosition = -3.0,  // Negative time positions sort before measure content
            Bounds = ClefCalculator.GetClefBounds(clef, context),
            Spacing = ClefCalculator.ClefSpacing(context)
        };
    }
}
