namespace StaffSharp.Layout.Model
{
    internal interface ILayoutElement
    {
        Bounds Bounds { get; set; }

        /// <summary>
        /// Offsets all positions (X, Y) in this element by the given amounts.
        /// Used to shift content when adjusting layout bounds.
        /// </summary>
        void Offset(double dx, double dy);
    }
}