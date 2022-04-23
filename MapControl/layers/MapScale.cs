// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Globalization;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Draws a map scale overlay.
/// </summary>
public class MapScale : MapOverlay
{
    #region Padding property

    public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(
        nameof(Padding), typeof(Thickness), typeof(MapScale), new PropertyMetadata(new Thickness(4)));

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    #endregion

    private readonly Polyline _line = new();

    private readonly TextBlock _label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        TextAlignment = TextAlignment.Center
    };

    public MapScale()
    {
        MinWidth = 100d;
        Children.Add(_line);
        Children.Add(_label);
    }

    protected override void SetParentMap(MapBase? map)
    {
        base.SetParentMap(map);

        _line.SetBinding(Shape.StrokeProperty, this.GetOrCreateBinding(StrokeProperty, nameof(Stroke)));
        _line.SetBinding(Shape.StrokeThicknessProperty, this.GetOrCreateBinding(StrokeThicknessProperty, nameof(StrokeThickness)));
        _label.SetBinding(TextBlock.ForegroundProperty, this.GetOrCreateBinding(ForegroundProperty, nameof(Foreground)));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size();

        if( ParentMap == null || ParentMap.Center == null )
            return size;

        var scale = ParentMap.GetScale(ParentMap.Center).X;
        var length = MinWidth / scale;
        var magnitude = Math.Pow(10d, Math.Floor(Math.Log10(length)));

        length = length / magnitude < 2d ? 2d * magnitude
            : length / magnitude < 5d ? 5d * magnitude
            : 10d * magnitude;

        size.Width = length * scale + StrokeThickness + Padding.Left + Padding.Right;
        size.Height = 1.25 * FontSize + StrokeThickness + Padding.Top + Padding.Bottom;

        var x1 = Padding.Left + StrokeThickness / 2d;
        var x2 = size.Width - Padding.Right - StrokeThickness / 2d;
        var y1 = size.Height / 2d;
        var y2 = size.Height - Padding.Bottom - StrokeThickness / 2d;

        _line.Points = new PointCollection
        {
            new Point(x1, y1),
            new Point(x1, y2),
            new Point(x2, y2),
            new Point(x2, y1)
        };

        _line.Measure(size);

        _label.Text = length >= 1000d
            ? string.Format(CultureInfo.InvariantCulture, "{0:0} km", length / 1000d)
            : string.Format(CultureInfo.InvariantCulture, "{0:0} m", length);
        _label.Width = size.Width;
        _label.Height = size.Height;
        _label.Measure(size);

        return size;
    }

    protected override void OnViewportChanged(ViewportChangedEventArgs e)
    {
        InvalidateMeasure();
    }
}