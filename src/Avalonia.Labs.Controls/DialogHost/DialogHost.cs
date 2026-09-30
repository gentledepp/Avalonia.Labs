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

    /// <summary>
    /// Hands the dialog the area it should keep its card clear of: the safe area, and whatever the
    /// on-screen keyboard actually covers.
    /// </summary>
    /// <remarks>
    /// The insets go to the dialog's card and nowhere else. This host carries the dialog, and the
    /// dialog paints its scrim inside its own template, so padding either of them dims a band of
    /// the screen with the card centred in the band rather than covering the viewport.
    /// </remarks>
    private void UpdateDialogInsets()
    {
        if (Content is not ContentDialog dialog)
            return;

        var safe = _insetsManager?.SafeAreaPadding ?? default;
        var covered = CoveredHeight();

        dialog.SetCurrentValue(
            ContentDialog.DialogInsetsProperty,
            new Thickness(safe.Left, safe.Top, safe.Right, covered > 0 ? covered + KeyboardGap : safe.Bottom));
    }

    /// <summary>
    /// How much of the viewport the on-screen keyboard actually covers.
    /// </summary>
    /// <remarks>
    /// Not <c>OccludedRect.Height</c>: the rect is also published when the keyboard is dismissed -
    /// on iOS as its off-screen frame, which still has a full height - so the height on its own
    /// says nothing about whether anything is covered, and a dismissed keyboard would keep the
    /// dialog permanently inset. Intersecting the rect with the client area answers the question
    /// that is actually being asked, and it also copes with a pane that only partly overlaps and
    /// with a platform that reports the rect in an unexpected coordinate space.
    /// </remarks>
    private double CoveredHeight()
    {
        if (_inputPane is not { State: InputPaneState.Open } pane)
            return 0;

        if (TopLevel.GetTopLevel(this) is not { } topLevel)
            return 0;

        var rect = pane.OccludedRect;
        return Math.Max(0, Math.Min(rect.Bottom, topLevel.ClientSize.Height) - Math.Max(rect.Top, 0));
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
