﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using J4JSoftware.XamlMapControl;
#if !UWP
using System.Windows;
#endif

namespace MapControl.Projections
{
    /// <summary>
    /// Elliptical Polar Stereographic Projection with a given scale factor at the pole and
    /// optional false easting and northing, as used by the UPS North and UPS South projections.
    /// See "Map Projections - A Working Manual" (https://pubs.usgs.gov/pp/1395/report.pdf), p.160-162.
    /// </summary>
    public class PolarStereographicProjection : MapProjection
    {
        private const double ConvergenceTolerance = 1e-6;
        private const int MaxIterations = 10;

        public bool IsNorth { get; }
        public double ScaleFactor { get; }
        public double FalseEasting { get; }
        public double FalseNorthing { get; }

        public PolarStereographicProjection(bool isNorth, double scaleFactor, double falseEasting, double falseNorthing)
        {
            Type = MapProjectionType.Azimuthal;
            IsNorth = isNorth;
            ScaleFactor = scaleFactor;
            FalseEasting = falseEasting;
            FalseNorthing = falseNorthing;
        }

        public override Vector GetRelativeScale(Location location)
        {
            var lat = (IsNorth ? location.Latitude : -location.Latitude) * Math.PI / 180d;
            var a = Wgs84EquatorialRadius;
            var e = Wgs84Eccentricity;
            var s = Math.Sqrt(Math.Pow(1 + e, 1 + e) * Math.Pow(1 - e, 1 - e));
            var t = Math.Tan(Math.PI / 4d - lat / 2d) / ConformalFactor(lat);
            var rho = 2d * a * ScaleFactor * t / s;
            var eSinLat = e * Math.Sin(lat);
            var m = Math.Cos(lat) / Math.Sqrt(1d - eSinLat * eSinLat);
            var k = rho / (a * m);

            return new Vector(k, k);
        }

        public override Point LocationToMap(Location location)
        {
            var lat = location.Latitude * Math.PI / 180d;
            var lon = location.Longitude * Math.PI / 180d;

            if (IsNorth)
            {
                lon = Math.PI - lon;
            }
            else
            {
                lat = -lat;
            }

            var a = Wgs84EquatorialRadius;
            var e = Wgs84Eccentricity;
            var s = Math.Sqrt(Math.Pow(1 + e, 1 + e) * Math.Pow(1 - e, 1 - e));
            var t = Math.Tan(Math.PI / 4d - lat / 2d) / ConformalFactor(lat);
            var rho = 2d * a * ScaleFactor * t / s;

            return new Point(rho * Math.Sin(lon) + FalseEasting, rho * Math.Cos(lon) + FalseNorthing);
        }

        public override Location MapToLocation(Point point)
        {
            point.X -= FalseEasting;
            point.Y -= FalseNorthing;

            var lon = Math.Atan2(point.X, point.Y);
            var rho = Math.Sqrt(point.X * point.X + point.Y * point.Y);
            var a = Wgs84EquatorialRadius;
            var e = Wgs84Eccentricity;
            var s = Math.Sqrt(Math.Pow(1 + e, 1 + e) * Math.Pow(1 - e, 1 - e));
            var t = rho * s / (2d * a * ScaleFactor);
            var lat = Math.PI / 2d - 2d * Math.Atan(t);
            var relChange = 1d;

            for (int i = 0; i < MaxIterations && relChange > ConvergenceTolerance; i++)
            {
                var newLat = Math.PI / 2d - 2d * Math.Atan(t * ConformalFactor(lat));
                relChange = Math.Abs(1d - newLat / lat);
                lat = newLat;
            }

            if (IsNorth)
            {
                lon = Math.PI - lon;
            }
            else
            {
                lat = -lat;
            }

            return new Location(lat * 180d / Math.PI, lon * 180d / Math.PI);
        }

        private static double ConformalFactor(double lat)
        {
            var eSinLat = Wgs84Eccentricity * Math.Sin(lat);

            return Math.Pow((1d - eSinLat) / (1d + eSinLat), Wgs84Eccentricity / 2d);
        }
    }

    /// <summary>
    /// Universal Polar Stereographic North Projection - EPSG:32661.
    /// </summary>
    public class UpsNorthProjection : PolarStereographicProjection
    {
        public const string DefaultCrsId = "EPSG:32661";

        public UpsNorthProjection()
            : base(true, 0.994, 2e6, 2e6)
        {
            CrsId = DefaultCrsId;
        }
    }

    /// <summary>
    /// Universal Polar Stereographic South Projection - EPSG:32761.
    /// </summary>
    public class UpsSouthProjection : PolarStereographicProjection
    {
        public const string DefaultCrsId = "EPSG:32761";

        public UpsSouthProjection()
            : base(false, 0.994, 2e6, 2e6)
        {
            CrsId = DefaultCrsId;
        }
    }
}
