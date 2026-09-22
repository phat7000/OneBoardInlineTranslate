using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using OneBoardInlineTranslate.Models;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace OneBoardInlineTranslate.Views;

public partial class RegionSelectionWindow : Window
{
    private Point? _start;

    internal RegionSelectionWindow()
    {
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    internal ScreenRegion? SelectedRegion { get; private set; }

    private void Window_KeyDown(object sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key == Key.Escape)
        {
            DialogResult = false;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
    {
        _start = eventArgs.GetPosition(this);
        SelectionRectangle.Visibility = Visibility.Visible;
        CaptureMouse();
        UpdateRectangle(_start.Value, _start.Value);
    }

    private void Window_MouseMove(object sender, MouseEventArgs eventArgs)
    {
        if (_start is not null && eventArgs.LeftButton == MouseButtonState.Pressed)
        {
            UpdateRectangle(_start.Value, eventArgs.GetPosition(this));
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs eventArgs)
    {
        if (_start is null)
        {
            return;
        }

        var end = eventArgs.GetPosition(this);
        var physicalStart = PointToScreen(_start.Value);
        var physicalEnd = PointToScreen(end);
        var region = ScreenRegion.FromPoints(
            (int)Math.Round(physicalStart.X),
            (int)Math.Round(physicalStart.Y),
            (int)Math.Round(physicalEnd.X),
            (int)Math.Round(physicalEnd.Y));
        ReleaseMouseCapture();
        SelectedRegion = region.IsUsable ? region : null;
        DialogResult = SelectedRegion is not null;
    }

    private void UpdateRectangle(Point start, Point end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        Canvas.SetLeft(SelectionRectangle, left);
        Canvas.SetTop(SelectionRectangle, top);
        SelectionRectangle.Width = Math.Abs(end.X - start.X);
        SelectionRectangle.Height = Math.Abs(end.Y - start.Y);
    }
}
