namespace StaffSharp;

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