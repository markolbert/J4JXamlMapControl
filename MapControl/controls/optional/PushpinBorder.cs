// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace J4JSoftware.XamlMapControl;

[ContentProperty(Name = "Child")]
public class PushpinBorder : UserControl
{
    #region ArrowSize property

    public static readonly DependencyProperty ArrowSizeProperty = DependencyProperty.Register( 
        nameof( ArrowSize ),
        typeof( Size ),
        typeof( PushpinBorder ),
        new PropertyMetadata( new Size( 10d, 20d ), ( o, _ ) => ( (PushpinBorder) o ).SetBorderMargin() ) );

    public Size ArrowSize
    {
        get => (Size)GetValue(ArrowSizeProperty);
        set => SetValue(ArrowSizeProperty, value);
    }

    #endregion

    #region BorderWidth property

    public static readonly DependencyProperty BorderWidthProperty = DependencyProperty.Register( 
        nameof( BorderWidth ),
        typeof( double ),
        typeof( PushpinBorder ),
        new PropertyMetadata( 0d, ( o, _ ) => ( (PushpinBorder) o ).SetBorderMargin() ) );

    public double BorderWidth
    {
        get => (double)GetValue(BorderWidthProperty);
        set => SetValue(BorderWidthProperty, value);
    }

    #endregion

    private readonly Border _border = new();

    public PushpinBorder()
    {
        var path = new Path
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Stretch = Stretch.None
        };

        path.SetBinding(Shape.FillProperty, new Binding
        {
            Path = new PropertyPath("Background"),
            Source = this
        });

        path.SetBinding(Shape.StrokeProperty, new Binding
        {
            Path = new PropertyPath("BorderBrush"),
            Source = this
        });

        path.SetBinding(Shape.StrokeThicknessProperty, new Binding
        {
            Path = new PropertyPath("BorderThickness"),
            Source = this
        });

        _border.SetBinding(PaddingProperty, new Binding
        {
            Path = new PropertyPath("Padding"),
            Source = this
        });

        SetBorderMargin();

        var grid = new Grid();
        grid.Children.Add(path);
        grid.Children.Add(_border);

        Content = grid;

        SizeChanged += (_, _) => path.Data = BuildGeometry();
    }

    private void SetBorderMargin()
    {
        _border.Margin = new Thickness(
            BorderWidth, BorderWidth, BorderWidth, BorderWidth + ArrowSize.Height);
    }

    public UIElement Child
    {
        get => _border.Child;
        set => _border.Child = value;
    }

    protected virtual Geometry BuildGeometry()
    {
        var width = Math.Floor(RenderSize.Width);
        var height = Math.Floor(RenderSize.Height);
        var x1 = BorderWidth / 2d;
        var y1 = BorderWidth / 2d;
        var x2 = width - x1;
        var y3 = height - y1;
        var y2 = y3 - ArrowSize.Height;
        var aw = ArrowSize.Width;
        var r1 = CornerRadius.TopLeft;
        var r2 = CornerRadius.TopRight;
        var r3 = CornerRadius.BottomRight;
        var r4 = CornerRadius.BottomLeft;

        var figure = new PathFigure
        {
            StartPoint = new Point(x1, y1 + r1),
            IsClosed = true,
            IsFilled = true
        };

        figure.Segments.Add(ArcTo(x1 + r1, y1, r1));
        figure.Segments.Add(LineTo(x2 - r2, y1));
        figure.Segments.Add(ArcTo(x2, y1 + r2, r2));

        if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            figure.Segments.Add(LineTo(x2, y3));
            figure.Segments.Add(LineTo(x2 - aw, y2));
        }
        else
        {
            figure.Segments.Add(LineTo(x2, y2 - r3));
            figure.Segments.Add(ArcTo(x2 - r3, y2, r3));
        }

        if (HorizontalAlignment == HorizontalAlignment.Center)
        {
            var c = width / 2d;
            figure.Segments.Add(LineTo(c + aw / 2d, y2));
            figure.Segments.Add(LineTo(c, y3));
            figure.Segments.Add(LineTo(c - aw / 2d, y2));
        }

        if (HorizontalAlignment == HorizontalAlignment.Left || HorizontalAlignment == HorizontalAlignment.Stretch)
        {
            figure.Segments.Add(LineTo(x1 + aw, y2));
            figure.Segments.Add(LineTo(x1, y3));
        }
        else
        {
            figure.Segments.Add(LineTo(x1 + r4, y2));
            figure.Segments.Add(ArcTo(x1, y2 - r4, r4));
        }

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        return geometry;
    }

    private LineSegment LineTo( double x, double y ) => new() { Point = new Point( x, y ) };

    private ArcSegment ArcTo( double x, double y, double r ) =>
        new() { Point = new Point( x, y ), Size = new Size( r, r ), SweepDirection = SweepDirection.Clockwise };
}