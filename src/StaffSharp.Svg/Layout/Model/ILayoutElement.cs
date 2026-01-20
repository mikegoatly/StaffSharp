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

    internal readonly record struct Bounds(double X, double Y, double Width, double Height)
    {
        internal double X2 => X + Width;
        internal double Y2 => Y + Height;

        internal Bounds AtZero()
        {
            return this with { X = 0, Y = 0 };
        }

        internal Bounds Offset(double dx, double dy)
        {
            return this with { X = X + dx, Y = Y + dy };
        }
    }
}