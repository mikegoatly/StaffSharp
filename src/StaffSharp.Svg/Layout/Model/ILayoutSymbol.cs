namespace StaffSharp.Layout.Model
{
    internal interface ILayoutSymbol : ILayoutElement
    {
        int LedgerLineCount { get; set; }
        bool LedgerLinesAbove { get; set; }
        double FirstLedgerLineOffsetY { get; set; }
        LayoutSpacing Spacing { get; set; }
        double TimePosition { get; set; }
        int VoiceNumber { get; set; }
    }
}