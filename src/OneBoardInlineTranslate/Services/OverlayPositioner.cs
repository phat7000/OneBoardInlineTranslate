namespace OneBoardInlineTranslate.Services;

internal readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    internal int Width => Right - Left;

    internal int Height => Bottom - Top;
}

internal static class OverlayPositioner
{
    internal static (int X, int Y) Place(
        PixelRect source,
        PixelRect workArea,
        int overlayWidth,
        int overlayHeight,
        int offset = 16)
    {
        var desiredX = source.Right - overlayWidth;
        var desiredY = source.Top + 44;
        var maximumX = workArea.Right - overlayWidth - offset;
        var maximumY = workArea.Bottom - overlayHeight - offset;
        var x = Math.Clamp(desiredX, workArea.Left + offset, Math.Max(workArea.Left + offset, maximumX));
        var y = Math.Clamp(desiredY, workArea.Top + offset, Math.Max(workArea.Top + offset, maximumY));
        return (x, y);
    }
}
