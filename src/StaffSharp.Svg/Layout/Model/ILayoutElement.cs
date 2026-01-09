namespace StaffSharp.Layout.Model
{
    internal interface ILayoutElement
    {
        double X { get; set; }
        double Y { get; set; }
        double Height { get; set; }
        double Width { get; set; }
    }
}