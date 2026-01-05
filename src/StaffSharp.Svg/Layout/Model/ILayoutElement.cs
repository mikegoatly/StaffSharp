namespace StaffSharp.Layout.Model
{
    public interface ILayoutElement
    {
        double X { get; set; }
        double Y { get; set; }
        double Height { get; set; }
        double Width { get; set; }
    }
}