using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.OCR;

internal interface IScreenCaptureService
{
    BitmapSource Capture(ScreenRegion region);
}

internal sealed class ScreenCaptureService : IScreenCaptureService
{
    private const int SrcCopy = 0x00CC0020;
    private const int CaptureBlt = 0x40000000;

    public BitmapSource Capture(ScreenRegion region)
    {
        if (!region.IsUsable)
        {
            throw new ArgumentOutOfRangeException(nameof(region));
        }

        var screenDc = GetDC(nint.Zero);
        if (screenDc == nint.Zero)
        {
            throw new InvalidOperationException("The screen device context is unavailable.");
        }

        var memoryDc = CreateCompatibleDC(screenDc);
        var bitmap = CreateCompatibleBitmap(screenDc, region.Width, region.Height);
        var previous = nint.Zero;
        try
        {
            if (memoryDc == nint.Zero || bitmap == nint.Zero)
            {
                throw new InvalidOperationException("The screen capture buffer could not be created.");
            }

            previous = SelectObject(memoryDc, bitmap);
            if (!BitBlt(
                    memoryDc,
                    0,
                    0,
                    region.Width,
                    region.Height,
                    screenDc,
                    region.X,
                    region.Y,
                    SrcCopy | CaptureBlt))
            {
                throw new InvalidOperationException("Windows could not capture the selected region.");
            }

            var source = Imaging.CreateBitmapSourceFromHBitmap(
                bitmap,
                nint.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            if (previous != nint.Zero && memoryDc != nint.Zero)
            {
                SelectObject(memoryDc, previous);
            }

            if (bitmap != nint.Zero)
            {
                DeleteObject(bitmap);
            }

            if (memoryDc != nint.Zero)
            {
                DeleteDC(memoryDc);
            }

            ReleaseDC(nint.Zero, screenDc);
        }
    }

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint deviceContext);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleDC(nint deviceContext);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(nint deviceContext);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleBitmap(nint deviceContext, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern nint SelectObject(nint deviceContext, nint value);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint value);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(
        nint destination,
        int xDestination,
        int yDestination,
        int width,
        int height,
        nint source,
        int xSource,
        int ySource,
        int operation);
}
