namespace OneBoardInlineTranslate.Services;

using OneBoardInlineTranslate.Models;

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

    internal static (int X, int Y) Place(
        PopupPositionMode mode,
        PixelRect source,
        PixelRect workArea,
        int overlayWidth,
        int overlayHeight,
        int? customX = null,
        int? customY = null,
        int offset = 16)
    {
        var desired = mode switch
        {
            PopupPositionMode.TopRight => (workArea.Right - overlayWidth - offset, workArea.Top + offset),
            PopupPositionMode.BottomRight => (workArea.Right - overlayWidth - offset, workArea.Bottom - overlayHeight - offset),
            PopupPositionMode.TopLeft => (workArea.Left + offset, workArea.Top + offset),
            PopupPositionMode.BottomLeft => (workArea.Left + offset, workArea.Bottom - overlayHeight - offset),
            PopupPositionMode.Custom when customX.HasValue && customY.HasValue => (customX.Value, customY.Value),
            _ => (source.Right - overlayWidth, source.Bottom + 10)
        };
        return Clamp(workArea, overlayWidth, overlayHeight, desired.Item1, desired.Item2, offset);
    }

    internal static PixelRect ClampBounds(PixelRect bounds, PixelRect workArea, int minimumWidth = 340, int minimumHeight = 220)
    {
        var width = Math.Clamp(bounds.Width, minimumWidth, Math.Max(minimumWidth, workArea.Width));
        var height = Math.Clamp(bounds.Height, minimumHeight, Math.Max(minimumHeight, workArea.Height));
        var (x, y) = Clamp(workArea, width, height, bounds.Left, bounds.Top, 0);
        return new PixelRect(x, y, x + width, y + height);
    }

    private static (int X, int Y) Clamp(
        PixelRect workArea,
        int overlayWidth,
        int overlayHeight,
        int desiredX,
        int desiredY,
        int offset)
    {
        var maximumX = workArea.Right - overlayWidth - offset;
        var maximumY = workArea.Bottom - overlayHeight - offset;
        var x = Math.Clamp(desiredX, workArea.Left + offset, Math.Max(workArea.Left + offset, maximumX));
        var y = Math.Clamp(desiredY, workArea.Top + offset, Math.Max(workArea.Top + offset, maximumY));
        return (x, y);
    }
}
