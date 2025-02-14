﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;

namespace J4JSoftware.XamlMapControl
{
    /// <summary>
    /// Elliptical Mercator Projection - EPSG:3395.
    /// See "Map Projections - A Working Manual" (https://pubs.usgs.gov/pp/1395/report.pdf), p.44-45.
    /// </summary>
    public class WorldMercatorProjection : MapProjection
    {
        private const double ConvergenceTolerance = 1e-6;
        private const int MaxIterations = 10;

        public const string DefaultCrsId = "EPSG:3395";

        public WorldMercatorProjection()
        {
            Type = MapProjectionType.NormalCylindrical;
            CrsId = DefaultCrsId;
        }

        public override Vector GetRelativeScale(Location location)
        {
            var lat = location.Latitude * Math.PI / 180d;
            var eSinLat = Wgs84Eccentricity * Math.Sin(lat);
            var k = Math.Sqrt(1d - eSinLat * eSinLat) / Math.Cos(lat); // p.44 (7-8)

            return new Vector(k, k);
        }

        public override Point LocationToMap(Location location)
        {
            return new Point(
                Wgs84MeterPerDegree * location.Longitude,
                Wgs84MeterPerDegree * LatitudeToY(location.Latitude));
        }

        public override Location MapToLocation(Point point)
        {
            return new Location(
                YToLatitude(point.Y / Wgs84MeterPerDegree),
                point.X / Wgs84MeterPerDegree);
        }

        public static double LatitudeToY(double latitude)
        {
            if (latitude <= -90d)
            {
                return double.NegativeInfinity;
            }

            if (latitude >= 90d)
            {
                return double.PositiveInfinity;
            }

            var lat = latitude * Math.PI / 180d;

            return Math.Log(Math.Tan(lat / 2d + Math.PI / 4d) * ConformalFactor(lat)) * 180d / Math.PI; // p.44 (7-7)
        }

        public static double YToLatitude(double y)
        {
            var t = Math.Exp(-y * Math.PI / 180d); // p.44 (7-10)
            var lat = Math.PI / 2d - 2d * Math.Atan(t); // p.44 (7-11)
            var relChange = 1d;

            for (var i = 0; i < MaxIterations && relChange > ConvergenceTolerance; i++)
            {
                var newLat = Math.PI / 2d - 2d * Math.Atan(t * ConformalFactor(lat)); // p.44 (7-9)
                relChange = Math.Abs(1d - newLat / lat);
                lat = newLat;
            }

            return lat * 180d / Math.PI;
        }

        private static double ConformalFactor(double lat)
        {
            var eSinLat = Wgs84Eccentricity * Math.Sin(lat);

            return Math.Pow((1d - eSinLat) / (1d + eSinLat), Wgs84Eccentricity / 2d);
        }
    }
}
