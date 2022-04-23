// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;
using System.Globalization;
using Windows.Foundation;

namespace J4JSoftware.XamlMapControl.Projections;

/// <summary>
/// MapProjection based on ProjNET4GeoApi.
/// </summary>
public class GeoApiProjection : MapProjection
{
    private ICoordinateSystem? _coordinateSystem;
    private double _scaleFactor;
    private string? _bBoxFormat;

    public GeoApiProjection(string coordinateSystemWkt)
    {
        CoordinateSystemWkt = coordinateSystemWkt;
    }

    protected GeoApiProjection()
    {
    }

    /// <summary>
    /// Gets or sets an OGC Well-known text representation of a coordinate system,
    /// i.e. a PROJCS[...] or GEOGCS[...] string as used by https://epsg.io or http://spatialreference.org.
    /// Setting this property updates the CoordinateSystem property with an ICoordinateSystem created from the WKT string.
    /// </summary>
    public string? CoordinateSystemWkt
    {
        get => CoordinateSystem?.WKT ?? null;
        protected set => CoordinateSystem = new CoordinateSystemFactory().CreateFromWkt(value);
    }

    public IMathTransform? LocationToMapTransform { get; private set; }
    public IMathTransform? MapToLocationTransform { get; private set; }

    /// <summary>
    /// Gets or sets the ICoordinateSystem of the MapProjection.
    /// </summary>
    public ICoordinateSystem? CoordinateSystem
    {
        get => _coordinateSystem;

        protected set
        {
            _coordinateSystem = value ?? throw new ArgumentNullException(nameof(value));

            var transformFactory = new CoordinateTransformationFactory();

            LocationToMapTransform = transformFactory
                                    .CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, _coordinateSystem)
                                    .MathTransform;

            MapToLocationTransform = transformFactory
                                    .CreateFromCoordinateSystems(_coordinateSystem, GeographicCoordinateSystem.WGS84)
                                    .MathTransform;

            CrsId = (!string.IsNullOrEmpty(_coordinateSystem.Authority) && _coordinateSystem.AuthorityCode > 0)
                ? $"{_coordinateSystem.Authority}:{_coordinateSystem.AuthorityCode}"
                : "";

            var projection = (_coordinateSystem as IProjectedCoordinateSystem)?.Projection;

            if (projection != null)
            {
                var centralMeridian = projection.GetParameter("central_meridian") ?? projection.GetParameter("longitude_of_origin");
                var centralParallel = projection.GetParameter("central_parallel") ?? projection.GetParameter("latitude_of_origin");
                var falseEasting = projection.GetParameter("false_easting");
                var falseNorthing = projection.GetParameter("false_northing");

                if (CrsId == "EPSG:3857")
                    Type = MapProjectionType.WebMercator;
                else if (
                        (centralMeridian == null || centralMeridian.Value == 0d) &&
                        (centralParallel == null || centralParallel.Value == 0d) &&
                        (falseEasting == null || falseEasting.Value == 0d) &&
                        (falseNorthing == null || falseNorthing.Value == 0d))
                    {
                        Type = MapProjectionType.NormalCylindrical;
                    }
                    else if (
                        projection.Name.StartsWith("UTM") ||
                        projection.Name.StartsWith("Transverse"))
                    {
                        Type = MapProjectionType.TransverseCylindrical;
                    }

                _scaleFactor = 1d;
                _bBoxFormat = "{0},{1},{2},{3}";
            }
            else
            {
                Type = MapProjectionType.NormalCylindrical;
                _scaleFactor = Wgs84MeterPerDegree;
                _bBoxFormat = "{1},{0},{3},{2}";
            }
        }
    }

    public override Point LocationToMap(Location? location)
    {
        if (LocationToMapTransform == null)
            throw new InvalidOperationException("The CoordinateSystem property is not set.");

        if( location == null )
            return new Point();

        var coordinate = LocationToMapTransform.Transform(
            new Coordinate(location.Longitude, location.Latitude));

        return new Point(coordinate.X * _scaleFactor, coordinate.Y * _scaleFactor);
    }

    public override Location MapToLocation(Point point)
    {
        if (MapToLocationTransform == null)
            throw new InvalidOperationException("The CoordinateSystem property is not set.");

        var coordinate = MapToLocationTransform.Transform(
            new Coordinate(point.X / _scaleFactor, point.Y / _scaleFactor));

        return new Location(coordinate.Y, coordinate.X);
    }

    public override string GetBboxValue(Rect rect) =>
        string.Format(CultureInfo.InvariantCulture, _bBoxFormat!,
                      rect.X / _scaleFactor, rect.Y / _scaleFactor,
                      (rect.X + rect.Width) / _scaleFactor, (rect.Y + rect.Height) / _scaleFactor);
}