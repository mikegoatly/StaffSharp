using StaffSharp.Notation;

namespace StaffSharp.Layout.Services
{
    internal static class TimeSignatureCalculator
    {
        public static double CalculateWidth(TimeSignature timeSignature, SvgContext context)
        {
            return 1.8 * context.StaffSpace;
        }
    }
}
