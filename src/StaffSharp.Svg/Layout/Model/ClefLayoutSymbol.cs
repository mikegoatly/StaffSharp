namespace StaffSharp.Layout.Model;

using System;

using StaffSharp;

using StaffSharp.Layout.Services;
using StaffSharp.Notation;

/// <summary>
/// Represents a positioned clef.
/// </summary>
public sealed class ClefLayoutSymbol : LayoutSymbol
{
    public required Clef Clef { get; init; }

    internal static LayoutSymbol Create(Clef clef, SvgContext context)
    {
        return new ClefLayoutSymbol
        {
            Clef = clef,
            TimePosition = -3.0,  // Negative time positions sort before measure content
            Width = ClefCalculator.GetClefWidth(clef, context),
            Y = ClefCalculator.GetClefYPosition(clef, context),
            Spacing = ClefCalculator.ClefSpacing(context)
        };
    }
}
