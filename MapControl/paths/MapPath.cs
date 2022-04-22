// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using J4JSoftware.XamlMapControl.Projections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// A path element with a Data property that holds a Geometry in view coordinates or
/// cartesian map coordinates that are relative to an origin Location.
/// </summary>
public class MapPath : Path, IMapElement
{
    #region Location property

    /// <summary>
    /// A Location that is used as
    /// - either the origin point of a geometry specified in cartesian map units (meters)
    /// - or as an optional value to constrain the view position of MapPaths with multiple
    ///   Locations (like MapPolyline or MapPolygon) to the visible map viewport, as done
    ///   for elements where the MapPanel.Location property is set.
    /// </summary>
    public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(
        nameof(Location), typeof(Location), typeof(MapPath),
        new PropertyMetadata(null, (o, _) => ((MapPath)o).UpdateData()));

    public MapPath()
    {
        MapPanel.InitMapElement(this);
    }

    public Location? Location
    {
        get => (Location?)GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    #endregion

    private MapBase? _parentMap;

    public MapBase? ParentMap
    {
        get => _parentMap;

        set
        {
            if (_parentMap != null)
                _parentMap.ViewportChanged -= OnViewportChanged;

            _parentMap = value;

            if (_parentMap != null)
                _parentMap.ViewportChanged += OnViewportChanged;

            UpdateData();
        }
    }

    private void OnViewportChanged(object? sender, ViewportChangedEventArgs e)
    {
        UpdateData();
    }

    protected virtual void UpdateData()
    {
        if (_parentMap == null || Data == null || Location == null)
            return;

        MapPanel.SetLocation( this, Location );

        var scale = _parentMap.GetScale(Location);
        var transform = new Matrix(scale.X, 0d, 0d, scale.Y, 0d, 0d);

        transform.Rotate(_parentMap.ViewTransform.Rotation);

        Data.Transform = new MatrixTransform { Matrix = transform };
    }

    #region Methods used only by derived classes MapPolyline, MapPolygon and MapMultiPolygon

    protected double GetLongitudeOffset(Location? location)
    {
        var longitudeOffset = 0d;

        if( location == null
        || _parentMap == null
        || _parentMap.MapProjection.Type > MapProjectionType.NormalCylindrical )
            return longitudeOffset;

        var pos = _parentMap.LocationToView(location);

        if (pos.X < 0d || pos.X > _parentMap.RenderSize.Width ||
            pos.Y < 0d || pos.Y > _parentMap.RenderSize.Height)
        {
            longitudeOffset = _parentMap.ConstrainedLongitude(location.Longitude) - location.Longitude;
        }

        return longitudeOffset;
    }

    protected Point LocationToMap( Location location, double longitudeOffset )
    {
        if( _parentMap == null )
            return new Point();

        if( longitudeOffset != 0d )
            location = new Location( location.Latitude, location.Longitude + longitudeOffset );

        var point = _parentMap.MapProjection.LocationToMap( location );

        if( double.IsPositiveInfinity( point.Y ) )
            point.Y = 1e9;
        else
            if( double.IsNegativeInfinity( point.X ) )
            {
                point.Y = -1e9;
            }

        return point;
    }

    protected Point LocationToView( Location location, double longitudeOffset ) =>
        _parentMap == null
            ? new Point()
            : _parentMap.ViewTransform.MapToView( LocationToMap( location, longitudeOffset ) );

    protected void DataCollectionPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= DataCollectionChanged;

        if (e.NewValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += DataCollectionChanged;

        UpdateData();
    }

    protected void DataCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateData();

    protected void AddPolylineLocations(PathFigureCollection pathFigures, IEnumerable<Location> locations, double longitudeOffset, bool closed)
    {
        var locList = locations.ToList();

        if( ParentMap == null || locList.Count < 2 )
            return;

        var points = locList.Select( location => LocationToView( location, longitudeOffset ) )
                            .ToList();

        if (closed)
        {
            var segment = new PolyLineSegment();

            foreach (var point in points.Skip(1))
            {
                segment.Points.Add(point);
            }

            var figure = new PathFigure
            {
                StartPoint = points.First(),
                IsClosed = closed,
                IsFilled = closed
            };

            figure.Segments.Add(segment);
            pathFigures.Add(figure);
        }
        else
        {
            if (closed)
                points.Add(points[0]);

            var viewport = new Rect(0, 0, ParentMap.RenderSize.Width, ParentMap.RenderSize.Height);
            PathFigure? figure = null;
            PolyLineSegment? segment = null;

            for (var i = 1; i < points.Count; i++)
            {
                var p1 = points[i - 1];
                var p2 = points[i];
                var inside = Intersections.GetIntersections(ref p1, ref p2, viewport);

                if (inside)
                {
                    if (figure == null)
                    {
                        figure = new PathFigure
                        {
                            StartPoint = p1,
                            IsClosed = false,
                            IsFilled = false
                        };

                        segment = new PolyLineSegment();
                        figure.Segments.Add(segment);
                        pathFigures.Add(figure);
                    }

                    segment!.Points.Add(p2);
                }

                if (!inside || p2 != points[i])
                {
                    figure = null;
                }
            }
        }
    }

    #endregion
}