namespace StaffSharp.Layout.Model;

using StaffSharp.Layout.Services;
using StaffSharp.Notation;

/// <summary>
/// Represents a positioned time signature.
/// </summary>
internal sealed class TimeSignatureLayoutSymbol : LayoutSymbol
{
    public required TimeSignature TimeSignature { get; init; }

    internal static TimeSignatureLayoutSymbol Create(TimeSignature timeSignature, SvgContext context)
    {
        return new TimeSignatureLayoutSymbol
        {
            TimeSignature = timeSignature,
            TimePosition = -1.0,
            Width = TimeSignatureCalculator.CalculateWidth(timeSignature, context),
        };
    }
}
