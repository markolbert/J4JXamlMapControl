// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Globalization;
using Windows.Foundation;
using J4JSoftware.XamlMapControl.Projections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Draws a graticule overlay.
/// </summary>
public class MapGraticule : MapOverlay
{
    public static readonly DependencyProperty MinLineDistanceProperty = DependencyProperty.Register(
        nameof(MinLineDistance), typeof(double), typeof(MapGraticule), new PropertyMetadata(150d));

    /// <summary>
    /// Minimum graticule line distance in pixels. The default value is 150.
    /// </summary>
    public double MinLineDistance
    {
        get => (double)GetValue(MinLineDistanceProperty);
        set => SetValue(MinLineDistanceProperty, value);
    }

    private Path? _path;

    public MapGraticule()
    {
        StrokeThickness = 0.5;
    }

    protected override void OnViewportChanged(ViewportChangedEventArgs e)
    {
        if( ParentMap == null )
            return;

        var map = ParentMap;
        var projection = map.MapProjection;

        if (projection.Type <= MapProjectionType.NormalCylindrical)
        {
            if( _path == null )
                InitializePath();

            var maxLocation = projection.MapToLocation(new Point(0d, 180d * MapProjection.Wgs84MeterPerDegree));
            var maxLatitude = maxLocation != null && maxLocation.Latitude < 90d ? maxLocation.Latitude : 90d;

            var bounds = map.ViewRectToBoundingBox(new Rect(0d, 0d, map.RenderSize.Width, map.RenderSize.Height));
            if( bounds == null )
                return;

            var lineDistance = GetLineDistance();

            var labelStart = new Location(
            Math.Ceiling(bounds.South / lineDistance) * lineDistance,
                Math.Ceiling(bounds.West / lineDistance) * lineDistance);

            var labelEnd = new Location(
                Math.Floor(bounds.North / lineDistance) * lineDistance,
                Math.Floor(bounds.East / lineDistance) * lineDistance);

            var lineStart = new Location(
                Math.Min(Math.Max(labelStart.Latitude - lineDistance, -maxLatitude), maxLatitude),
                labelStart.Longitude - lineDistance);

            var lineEnd = new Location(
                Math.Min(Math.Max(labelEnd.Latitude + lineDistance, -maxLatitude), maxLatitude),
                labelEnd.Longitude + lineDistance);

            var geometry = (PathGeometry)_path!.Data;
            geometry.Figures.Clear();

            for (var lat = labelStart.Latitude; lat <= bounds.North; lat += lineDistance)
            {
                var figure = new PathFigure
                {
                    StartPoint = map.LocationToView(new Location(lat, lineStart.Longitude)),
                    IsClosed = false,
                    IsFilled = false
                };

                figure.Segments.Add(new LineSegment
                {
                    Point = map.LocationToView(new Location(lat, lineEnd.Longitude))
                });

                geometry.Figures.Add(figure);
            }

            for (var lon = labelStart.Longitude; lon <= bounds.East; lon += lineDistance)
            {
                var figure = new PathFigure
                {
                    StartPoint = map.LocationToView(new Location(lineStart.Latitude, lon)),
                    IsClosed = false,
                    IsFilled = false
                };

                figure.Segments.Add(new LineSegment
                {
                    Point = map.LocationToView(new Location(lineEnd.Latitude, lon))
                });

                geometry.Figures.Add(figure);
            }

            var labelFormat = GetLabelFormat(lineDistance);
            var childIndex = 1; // 0 for Path

            for (var lat = labelStart.Latitude; lat <= bounds.North; lat += lineDistance)
            {
                for (var lon = labelStart.Longitude; lon <= bounds.East; lon += lineDistance)
                {
                    TextBlock label;

                    if (childIndex < Children.Count)
                        label = (TextBlock)Children[childIndex];
                    else
                    {
                        label = new TextBlock { RenderTransform = new MatrixTransform() };
                        label.SetBinding(TextBlock.FontSizeProperty, this.GetOrCreateBinding(FontSizeProperty, nameof(FontSize)));
                        label.SetBinding(TextBlock.FontStyleProperty, this.GetOrCreateBinding(FontStyleProperty, nameof(FontStyle)));
                        label.SetBinding(TextBlock.FontStretchProperty, this.GetOrCreateBinding(FontStretchProperty, nameof(FontStretch)));
                        label.SetBinding(TextBlock.FontWeightProperty, this.GetOrCreateBinding(FontWeightProperty, nameof(FontWeight)));
                        label.SetBinding(TextBlock.ForegroundProperty, this.GetOrCreateBinding(ForegroundProperty, nameof(Foreground)));

                        if (FontFamily != null)
                            label.SetBinding(TextBlock.FontFamilyProperty, this.GetOrCreateBinding(FontFamilyProperty, nameof(FontFamily)));

                        Children.Add(label);
                    }

                    childIndex++;

                    label.Text = GetLabelText(lat, labelFormat, "NS") + "\n" + GetLabelText(Location.NormalizeLongitude(lon), labelFormat, "EW");
                    label.Tag = new Location(lat, lon);
                    label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }

                while (Children.Count > childIndex)
                {
                    Children.RemoveAt(Children.Count - 1);
                }
            }

            // don't use MapPanel.Location because labels may be at more than 180° distance from map center
            for (int i = 1; i < Children.Count; i++)
            {
                var label = (TextBlock)Children[i];
                var location = (Location)label.Tag;
                var viewPosition = map.LocationToView(location);
                var matrix = new Matrix(1, 0, 0, 1, 0, 0);

                matrix.Translate(StrokeThickness / 2d + 2d, -label.DesiredSize.Height / 2d);
                matrix.Rotate(map.ViewTransform.Rotation);
                matrix.Translate(viewPosition.X, viewPosition.Y);

                ((MatrixTransform)label.RenderTransform).Matrix = matrix;
            }
        }
        else if (_path != null)
        {
            _path = null;
            Children.Clear();
        }

        base.OnViewportChanged(e);
    }

    private void InitializePath()
    {
        _path = new Path { Data = new PathGeometry() };
        _path.SetBinding( Shape.StrokeProperty, this.GetOrCreateBinding( StrokeProperty, nameof( Stroke ) ) );
        _path.SetBinding( Shape.StrokeThicknessProperty,
                          this.GetOrCreateBinding( StrokeThicknessProperty, nameof( StrokeThickness ) ) );
        _path.SetBinding( Shape.StrokeDashArrayProperty,
                          this.GetOrCreateBinding( StrokeDashArrayProperty, nameof( StrokeDashArray ) ) );
        _path.SetBinding( Shape.StrokeDashOffsetProperty,
                          this.GetOrCreateBinding( StrokeDashOffsetProperty, nameof( StrokeDashOffset ) ) );
        _path.SetBinding( Shape.StrokeDashCapProperty,
                          this.GetOrCreateBinding( StrokeDashCapProperty, nameof( StrokeDashCap ) ) );
        Children.Add( _path );
    }

    private double GetLineDistance()
    {
        var minDistance = MinLineDistance / PixelPerLongitudeDegree(ParentMap!.Center!);
        var scale = minDistance < 1d / 60d ? 3600d : minDistance < 1d ? 60d : 1d;
        minDistance *= scale;

        var lineDistances = new[] { 1d, 2d, 5d, 10d, 15d, 30d, 60d };
        var i = 0;

        while (i < lineDistances.Length - 1 && lineDistances[i] < minDistance)
        {
            i++;
        }

        return Math.Min(lineDistances[i] / scale, 30d);
    }

    private double PixelPerLongitudeDegree(Location location)
    {
        return Math.Max(1d, // a reasonable lower limit
                        ParentMap!.GetScale(location).X *
                        Math.Cos(location.Latitude * Math.PI / 180d) * MapProjection.Wgs84MeterPerDegree);
    }

    private static string GetLabelFormat(double lineDistance)
    {
        return lineDistance < 1d / 60d ? "{0} {1}°{2:00}'{3:00}\""
            : lineDistance < 1d ? "{0} {1}°{2:00}'" : "{0} {1}°";
    }

    private static string GetLabelText(double value, string format, string hemispheres)
    {
        var hemisphere = hemispheres[0];

        value = (value + 540d) % 360d - 180d;

        if (value < -1e-8) // ~1mm
        {
            value = -value;
            hemisphere = hemispheres[1];
        }

        var seconds = (int)Math.Round(value * 3600d);

        return string.Format( CultureInfo.InvariantCulture,
                              format,
                              hemisphere,
                              seconds / 3600,
                              seconds / 60 % 60,
                              seconds % 60 );
    }
}