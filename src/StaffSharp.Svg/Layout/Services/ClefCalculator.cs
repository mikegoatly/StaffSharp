namespace StaffSharp.Layout.Services;

using System;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

internal static class ClefCalculator
{
    public static double GetClefYPosition(Clef clef, SvgContext context)
    {
        // Position clef symbol (Y is relative to staff origin)
        // Treble clef: The spiral wraps around the G line (second line from bottom)
        // G line is at staff position +2 from baseline, which is Y = baseline - 1 staff space
        // Bass clef: The dots straddle the F line (fourth line from bottom, which is the baseline)
        if (clef == Clef.Treble)
        {
            // Position so the clef is centered vertically on the staff
            // Treble clef's defining point (where the spiral curl centers) should be at the G line
            // G line (second from bottom) is at Y = 30 (staff line index 3 * 10)
            return 3.0 * context.StaffSpace; // Position at G line
        }
        else if (clef == Clef.Bass)
        {
            // Bass clef dots should straddle the F line (second from top = baseline)
            return 2.0 * context.StaffSpace; // Position at middle line (F)
        }
        else
        {
            // TODO other clefs
            return 2.0 * context.StaffSpace; // Default to middle line
        }
    }

    public static LayoutSpacing ClefSpacing(SvgContext context)
    {
        return new LayoutSpacing(context.StaffSpace / 2.0);
    }

    public static double GetClefWidth(Clef clef, SvgContext context)
    {
        return 2.2 * context.StaffSpace;
    }
}