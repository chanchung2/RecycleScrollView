using System;

namespace RecycleScrollView
{
    [Serializable]
    public struct LayoutPadding
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;

        public LayoutPadding(int left, int right, int top, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }
    }
}
