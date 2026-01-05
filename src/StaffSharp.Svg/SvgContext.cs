namespace StaffSharp;

/// <summary>
/// Context for SVG export operations.
/// </summary>
public record SvgContext
{
    public int MaxWidth { get; set; }
    public double Scale { get; set; }
    public Margins Margins { get; set; }
    public int StaffSpace { get; set; }

    /// <summary>
    /// Horizontal spacing between notes in staff spaces.
    /// </summary>
    public double NoteSpacing { get; set; } = 2;
    public string? BailAfterPass { get; set; }
}

/// <summary>
/// Represents margins in pixels.
/// </summary>
public readonly record struct Margins
{
    public int Left { get; }
    public int Right { get; }
    public int Top { get; }
    public int Bottom { get; }

    public Margins(int left = 0, int right = 0, int top = 0, int bottom = 0)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }
}