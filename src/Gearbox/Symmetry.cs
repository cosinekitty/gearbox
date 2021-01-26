namespace Gearbox
{
    public enum Transform
    {
        Identity   = 0,
        LeftRight  = 1,
        WhiteBlack = 2,
        Diagonal   = 4,
        Maximum = LeftRight | WhiteBlack | Diagonal,
        Undefined  = 8,
    }

    public static class Symmetry
    {
        public static int ForwardTransform(int index, Transform transform)
        {
            int x = index % 8;   // file 0..7
            int y = index / 8;   // rank 0..7

            if (0 != (transform & Transform.LeftRight))
            {
                // Flip left and right sides of the board around a vertical axis.
                x = 7 - x;
            }

            if (0 != (transform & Transform.WhiteBlack))
            {
                // Flip White/Black sides of the board around a horizontal axis.
                y = 7 - y;
            }

            if (0 != (transform & Transform.Diagonal))
            {
                // Swap the x and y coordinates.
                int swap = x;
                x = y;
                y = swap;
            }

            // Convert (x=file, y=rank) back to index 0..63.
            return 8*y + x;
        }

        public static int InverseTransformIndex(int index, Transform transform)
        {
            int x = index % 8;   // file 0..7
            int y = index / 8;   // rank 0..7

            // The same as ForwardTransformIndex, only with the 3 steps in reverse order.

            if (0 != (transform & Transform.Diagonal))
            {
                // Swap the x and y coordinates.
                int swap = x;
                x = y;
                y = swap;
            }

            if (0 != (transform & Transform.WhiteBlack))
            {
                // Flip White/Black sides of the board around a horizontal axis.
                y = 7 - y;
            }

            if (0 != (transform & Transform.LeftRight))
            {
                // Flip left and right sides of the board around a vertical axis.
                x = 7 - x;
            }

            // Convert (x=file, y=rank) back to index 0..63.
            return 8*y + x;
        }
    }
}
