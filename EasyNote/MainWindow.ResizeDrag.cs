using System.Windows;
using System.Windows.Input;

namespace EasyNote;

public partial class MainWindow
{
    public void BeginDrag()
    {
        LogWindowEvent("BeginDrag.Start");
        try { DragMove(); } catch { }
        PersistWindowPlacement("BeginDrag");
        LogWindowEvent("BeginDrag.Done");
    }

    private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        _pendingDesktopReenterAfterResize = true;
        EnsureInteractiveMode();
        _isResizeDragging = true;
        _isTopResizeDragging = false;
        _resizeDragStartScreen = PointToScreen(e.GetPosition(this));
        _resizeDragStartHeight = Height;
        _resizeDragStartTop = Top;
        Mouse.Capture(sender as IInputElement);
        e.Handled = true;
        LogWindowEvent("ResizeGrip_MouseLeftButtonDown.Handled");
    }

    private void TopResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        BeginTopResizeGripDrag(sender, e);
        e.Handled = true;
    }

    private void BeginTopResizeGripDrag(object sender, MouseButtonEventArgs e)
    {
        _pendingDesktopReenterAfterResize = true;
        EnsureInteractiveMode();
        _isResizeDragging = true;
        _isTopResizeDragging = true;
        _resizeDragStartScreen = PointToScreen(e.GetPosition(this));
        _resizeDragStartHeight = Height;
        _resizeDragStartTop = Top;
        Mouse.Capture(sender as IInputElement);
        LogWindowEvent("TopResizeGrip_MouseLeftButtonDown.Handled");
    }

    private void TopResizeGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isTopResizeDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentScreen = PointToScreen(e.GetPosition(this));
        var deltaY = currentScreen.Y - _resizeDragStartScreen.Y;
        var nextHeight = Math.Clamp(_resizeDragStartHeight - deltaY, MinHeight, MaxHeight);
        var bottom = _resizeDragStartTop + _resizeDragStartHeight;

        Height = nextHeight;
        Top = bottom - nextHeight;
    }

    private void TopResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isTopResizeDragging)
        {
            return;
        }

        EndResizeGripDrag("TopResizeGrip_MouseLeftButtonUp");
        e.Handled = true;
    }

    private void TopResizeGrip_LostMouseCapture(object sender, MouseEventArgs e)
        => EndResizeGripDrag("TopResizeGrip_LostMouseCapture");

    private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isResizeDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentScreen = PointToScreen(e.GetPosition(this));
        var deltaY = currentScreen.Y - _resizeDragStartScreen.Y;
        Height = Math.Clamp(_resizeDragStartHeight + deltaY, MinHeight, MaxHeight);
    }

    private void ResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndResizeGripDrag("ResizeGrip_MouseLeftButtonUp");
        e.Handled = true;
    }

    private void ResizeGrip_LostMouseCapture(object sender, MouseEventArgs e)
        => EndResizeGripDrag("ResizeGrip_LostMouseCapture");

    private void EndResizeGripDrag(string reason)
    {
        if (!_isResizeDragging)
        {
            return;
        }

        _isResizeDragging = false;
        _isTopResizeDragging = false;
        if (Mouse.Captured is not null)
        {
            Mouse.Capture(null);
        }

        PersistWindowPlacement(reason);
        if (_pendingDesktopReenterAfterResize)
        {
            ReenterDesktopModeSoon();
            _pendingDesktopReenterAfterResize = false;
        }

        LogWindowEvent("EndResizeGripDrag", $"Reason={reason}");
    }
}
