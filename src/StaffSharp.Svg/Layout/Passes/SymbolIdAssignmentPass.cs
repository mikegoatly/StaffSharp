namespace StaffSharp.Layout.Passes;

/// <summary>
/// Assigns unique IDs to all symbols for highlighting and reference purposes.
/// </summary>
internal class SymbolIdAssignmentPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        int symbolCounter = 0;

        foreach (var symbol in model.Systems
            .SelectMany(s => s.Staves)
            .SelectMany(s => s.Measures)
            .SelectMany(s => s.Symbols))
        {
            symbol.Id = $"sym_{symbolCounter++}";
        }
    }
}
