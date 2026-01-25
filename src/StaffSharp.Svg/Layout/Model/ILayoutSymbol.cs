namespace StaffSharp.Layout.Model
{
    internal interface ILayoutSymbol : ILayoutElement
    {
        string Id { get; set; }
        int LedgerLineCount { get; set; }
        bool LedgerLinesAbove { get; set; }
        double FirstLedgerLineOffsetY { get; set; }
        LayoutSpacing Spacing { get; set; }
        double TimePosition { get; set; }
        int VoiceNumber { get; set; }
    }
}