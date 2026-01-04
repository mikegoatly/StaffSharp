namespace StaffSharp.Svg.Render;

using System.Globalization;
using System.Text;

/// <summary>
/// Builds SVG path strings for note flags procedurally.
/// </summary>
public static class FlagPathBuilder
{
    // Flag spacing constants (in SVG units)
    private const double NormalFlagSpacing = 1.4;
    private const double GraceFlagSpacing = 0.5;

    /// <summary>
    /// Builds an SVG path for note flags.
    /// </summary>
    /// <param name="flagCount">Number of flags (1 for eighth, 2 for sixteenth, etc.)</param>
    /// <param name="stemUp">True if stem points up, false if down</param>
    /// <param name="isGraceNote">True for grace note flags (smaller)</param>
    /// <param name="useStraightFlags">True for straight triangular flags, false for curved</param>
    /// <returns>SVG path data string</returns>
    public static string BuildFlagPath(int flagCount, bool stemUp, bool isGraceNote = false, bool useStraightFlags = false)
    {
        if (flagCount <= 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var spacing = isGraceNote ? GraceFlagSpacing : NormalFlagSpacing;

        if (useStraightFlags)
        {
            BuildStraightFlags(sb, flagCount, stemUp, spacing);
        }
        else if (isGraceNote)
        {
            BuildGraceCurvedFlags(sb, flagCount, stemUp, spacing);
        }
        else
        {
            BuildCurvedFlags(sb, flagCount, stemUp, spacing);
        }

        return sb.ToString();
    }

    private static void BuildCurvedFlags(StringBuilder sb, int flagCount, bool stemUp, double spacing)
    {
        sb.Append("M0 0");

        if (flagCount == 1)
        {
            // Single flag (eighth note) - larger curve
            if (stemUp)
            {
                sb.Append("c0.6 5.6 9.6 9 5.6 18.4 1.6 -6 -1.3 -11.6 -5.6 -12.8");
            }
            else
            {
                // Mirror vertically for stem down
                sb.Append("c0.6 -5.6 9.6 -9 5.6 -18.4 1.6 6 -1.3 11.6 -5.6 12.8");
            }
        }
        else
        {
            // Multiple flags (16th, 32nd, etc.) - smaller curves stacked
            for (int i = 0; i < flagCount; i++)
            {
                var yOffset = i * spacing;
                
                if (i > 0)
                {
                    // Move to next flag position
                    if (stemUp)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "m0 {0}", spacing);
                    }
                    else
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "m0 {0}", -spacing);
                    }
                }

                if (stemUp)
                {
                    sb.Append("c0.9 3.7 9.1 6.4 6 12.4 1 -5.4 -4.2 -8.4 -6 -8.4");
                }
                else
                {
                    // Mirror vertically for stem down
                    sb.Append("c0.9 -3.7 9.1 -6.4 6 -12.4 1 5.4 -4.2 8.4 -6 8.4");
                }
            }
        }
    }

    private static void BuildGraceCurvedFlags(StringBuilder sb, int flagCount, bool stemUp, double spacing)
    {
        sb.Append("M0 0");

        if (flagCount == 1)
        {
            // Single grace flag - scaled down
            if (stemUp)
            {
                sb.Append("c0.6 3.4 5.6 3.8 3 10 1.2 -4.4 -1.4 -7 -3 -7");
            }
            else
            {
                // Mirror vertically for stem down
                sb.Append("c0.6 -3.4 5.6 -3.8 3 -10 1.2 4.4 -1.4 7 -3 7");
            }
        }
        else
        {
            // Multiple grace flags - smaller spacing
            for (int i = 0; i < flagCount; i++)
            {
                if (i > 0)
                {
                    if (stemUp)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "m0 {0}", spacing);
                    }
                    else
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "m0 {0}", -spacing);
                    }
                }

                if (stemUp)
                {
                    sb.Append("c0.6 3.4 5.6 3.8 3 10 1.2 -4.4 -1.4 -7 -3 -7");
                }
                else
                {
                    sb.Append("c0.6 -3.4 5.6 -3.8 3 -10 1.2 4.4 -1.4 7 -3 7");
                }
            }
        }
    }

    private static void BuildStraightFlags(StringBuilder sb, int flagCount, bool stemUp, double spacing)
    {
        sb.Append("M0 0");

        for (int i = 0; i < flagCount; i++)
        {
            if (i > 0)
            {
                if (stemUp)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "m0 {0}", spacing);
                }
                else
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "m0 {0}", -spacing);
                }
            }

            if (stemUp)
            {
                sb.Append("l7 3.2 0 3.2 -7 -3.2z");
            }
            else
            {
                // Mirror vertically for stem down
                sb.Append("l7 -3.2 0 -3.2 -7 3.2z");
            }
        }
    }
}
