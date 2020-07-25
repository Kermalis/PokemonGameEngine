using Avalonia;
using Avalonia.Media;

namespace Kermalis.MapEditor.UI
{
    internal static class StandardGrid
    {
        private static Pen _pen;
        public static void CreatePen()
        {
            _pen = new Pen(new SolidColorBrush(0x60000000));
        }

        public static void RenderGrid(DrawingContext context, int width, int height, int itemWidth, int itemHeight)
        {
            for (int y = 0; y < height; y++)
            {
                int by = y * itemHeight;
                for (int x = 0; x < width; x++)
                {
                    int bx = x * itemWidth;
                    var r2 = new Rect(bx, by, itemWidth, itemHeight);
                    context.DrawRectangle(_pen, r2);
                }
            }
        }
    }
}
