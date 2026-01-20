namespace StaffSharp.Layout.Model
{
    internal interface ILayoutElement
    {
        Bounds Bounds { get; set; }

        /// <summary>
        /// Updates the bounds of this element based on its current properties.
        /// </summary>
        void UpdateBounds(SvgContext context);
    }

    internal readonly record struct Bounds(double X, double Y, double Width, double Height)
    {
        internal double X2 => X + Width;
        internal double Y2 => Y + Height;

        internal Bounds AtZero()
        {
            return this with { X = 0, Y = 0 };
        }
    }
}