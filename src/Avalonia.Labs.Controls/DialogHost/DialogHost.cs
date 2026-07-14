using System;
using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Reactive;

namespace Avalonia.Labs.Controls;

/// <summary>
/// Special control to host a <see cref="ContentDialog"/>/>
/// </summary>
public class DialogHost : ContentControl
{
    // Gap left between the bottom of the dialog and the on-screen keyboard.
    private const double KeyboardGap = 12;

    public DialogHost()
    {
        Background = null;
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
    }

    protected override Type StyleKeyOverride => typeof(OverlayPopupHost);

    protected override Size MeasureOverride(Size availableSize)
    {
        _ = base.MeasureOverride(availableSize);

        if (TopLevel.GetTopLevel(this) is { } tl)
        {
            return tl.ClientSize;
        }
        else if (VisualRoot is Control c)
        {
            return c.Bounds.Size;
        }

        return default;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (e.AttachmentPoint is Control wb)
        {
            // OverlayLayer is a Canvas, so we won't get a signal to resize if the window
            // bounds change. Subscribe to force update

            var observer = new AnonymousObserver<Rect>(_ => InvalidateMeasure());

            _rootBoundsWatcher = wb.GetObservable(BoundsProperty)
                .Subscribe(observer);
        }

        var topLevel = TopLevel.GetTopLevel(this);

        _inputPane = topLevel?.InputPane;
        if (_inputPane is not null)
            _inputPane.StateChanged += OnInputPaneStateChanged;

        _insetsManager = topLevel?.InsetsManager;
        if (_insetsManager is not null)
            _insetsManager.SafeAreaChanged += OnSafeAreaChanged;

        UpdateDialogInsets();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _rootBoundsWatcher?.Dispose();
        _rootBoundsWatcher = null;

        if (_inputPane is not null)
        {
            _inputPane.StateChanged -= OnInputPaneStateChanged;
            _inputPane = null;
        }

        if (_insetsManager is not null)
        {
            _insetsManager.SafeAreaChanged -= OnSafeAreaChanged;
            _insetsManager = null;
        }
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e) => UpdateDialogInsets();

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => UpdateDialogInsets();

    private void UpdateDialogInsets()
    {
        var safe = _insetsManager?.SafeAreaPadding ?? default;
        var occludedHeight = _inputPane?.OccludedRect.Height ?? 0;
        var bottom = safe.Bottom + (occludedHeight > 0 ? occludedHeight + KeyboardGap : 0);
        Padding = new Thickness(safe.Left, safe.Top, safe.Right, bottom);
        InvalidateMeasure();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        e.Handled = true;
    }

    private IDisposable? _rootBoundsWatcher;
    private IInputPane? _inputPane;
    private IInsetsManager? _insetsManager;
}
