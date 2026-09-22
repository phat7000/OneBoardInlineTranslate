namespace OneBoardInlineTranslate.Models;

internal readonly record struct ScreenRegion(int X, int Y, int Width, int Height)
{
    internal bool IsUsable => Width >= 8 && Height >= 8;

    internal static ScreenRegion FromPoints(int x1, int y1, int x2, int y2) => new(
        Math.Min(x1, x2),
        Math.Min(y1, y2),
        Math.Abs(x2 - x1),
        Math.Abs(y2 - y1));
}
