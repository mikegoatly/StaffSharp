using System.Numerics.Tensors;

namespace StaffSharp.Demo.Controls;

internal static class ArrayExtensions
{
    public static double[,] SwapDimensionsToDouble(this float[,] original)
    {
        var len0 = original.GetLength(0);
        var len1 = original.GetLength(1);

        var result = new double[len1, len0];  // [notes/Y, time/X]
        for (int t = 0; t < len0; t++)
        {
            for (int k = 0; k < len1; k++)
            {
                result[k, t] = original[t, k];  // Transpose: swap indices
            }
        }

        return result;
    }
}
