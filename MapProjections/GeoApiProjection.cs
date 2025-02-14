﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
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
using J4JSoftware.XamlMapControl;
using Location = J4JSoftware.XamlMapControl.Location;
using Point = J4JSoftware.XamlMapControl.Point;

namespace MapControl.Projections
{
    /// <summary>
    /// MapProjection based on ProjNET4GeoApi.
    /// </summary>
    public class GeoApiProjection : MapProjection
    {
        private ICoordinateSystem coordinateSystem;
        private double scaleFactor;
        private string bboxFormat;

        protected GeoApiProjection()
        {
        }

        public GeoApiProjection(string coordinateSystemWkt)
        {
            CoordinateSystemWkt = coordinateSystemWkt;
        }

        /// <summary>
        /// Gets or sets an OGC Well-known text representation of a coordinate system,
        /// i.e. a PROJCS[...] or GEOGCS[...] string as used by https://epsg.io or http://spatialreference.org.
        /// Setting this property updates the CoordinateSystem property with an ICoordinateSystem created from the WKT string.
        /// </summary>
        public string CoordinateSystemWkt
        {
            get { return CoordinateSystem?.WKT; }
            protected set { CoordinateSystem = new CoordinateSystemFactory().CreateFromWkt(value); }
        }

        /// <summary>
        /// Gets or sets the ICoordinateSystem of the MapProjection.
        /// </summary>
        public ICoordinateSystem CoordinateSystem
        {
            get { return coordinateSystem; }
            protected set
            {
                coordinateSystem = value ?? throw new ArgumentNullException(nameof(value));

                var transformFactory = new CoordinateTransformationFactory();

                LocationToMapTransform = transformFactory
                    .CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, coordinateSystem)
                    .MathTransform;

                MapToLocationTransform = transformFactory
                    .CreateFromCoordinateSystems(coordinateSystem, GeographicCoordinateSystem.WGS84)
                    .MathTransform;

                CrsId = (!string.IsNullOrEmpty(coordinateSystem.Authority) && coordinateSystem.AuthorityCode > 0)
                    ? string.Format("{0}:{1}", coordinateSystem.Authority, coordinateSystem.AuthorityCode)
                    : "";

                var projection = (coordinateSystem as IProjectedCoordinateSystem)?.Projection;

                if (projection != null)
                {
                    var centralMeridian = projection.GetParameter("central_meridian") ?? projection.GetParameter("longitude_of_origin");
                    var centralParallel = projection.GetParameter("central_parallel") ?? projection.GetParameter("latitude_of_origin");
                    var falseEasting = projection.GetParameter("false_easting");
                    var falseNorthing = projection.GetParameter("false_northing");

                    if (CrsId == "EPSG:3857")
                    {
                        Type = MapProjectionType.WebMercator;
                    }
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

                    scaleFactor = 1d;
                    bboxFormat = "{0},{1},{2},{3}";
                }
                else
                {
                    Type = MapProjectionType.NormalCylindrical;
                    scaleFactor = Wgs84MeterPerDegree;
                    bboxFormat = "{1},{0},{3},{2}";
                }
            }
        }

        public IMathTransform LocationToMapTransform { get; private set; }

        public IMathTransform MapToLocationTransform { get; private set; }

        public override Point LocationToMap(Location location)
        {
            if (LocationToMapTransform == null)
            {
                throw new InvalidOperationException("The CoordinateSystem property is not set.");
            }

            var coordinate = LocationToMapTransform.Transform(
                new Coordinate(location.Longitude, location.Latitude));

            return new Point(coordinate.X * scaleFactor, coordinate.Y * scaleFactor);
        }

        public override Location MapToLocation(Point point)
        {
            if (MapToLocationTransform == null)
            {
                throw new InvalidOperationException("The CoordinateSystem property is not set.");
            }

            var coordinate = MapToLocationTransform.Transform(
                new Coordinate(point.X / scaleFactor, point.Y / scaleFactor));

            return new Location(coordinate.Y, coordinate.X);
        }

        public override string GetBboxValue(Rect rect)
        {
            return string.Format(CultureInfo.InvariantCulture, bboxFormat,
                rect.X / scaleFactor, rect.Y / scaleFactor,
                (rect.X + rect.Width) / scaleFactor, (rect.Y + rect.Height) / scaleFactor);
        }
    }
}
