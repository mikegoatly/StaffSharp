namespace StaffSharp.Layout.Model
{
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

        internal Bounds RelativeTo(Bounds bounds)
        {
            return this with { X = X - bounds.X, Y = Y - bounds.Y };
        }
    }
}