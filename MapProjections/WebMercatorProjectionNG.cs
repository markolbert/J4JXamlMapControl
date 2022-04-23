// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using ProjNet.CoordinateSystems;
using System;

namespace J4JSoftware.XamlMapControl.Projections
{
    /// <summary>
    /// Spherical Mercator Projection implemented by setting the CoordinateSystem property of a GeoApiProjection.
    /// See "Map Projections - A Working Manual" (https://pubs.usgs.gov/pp/1395/report.pdf), p.41-44.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class WebMercatorProjectionNG : GeoApiProjection
    {
        public WebMercatorProjectionNG()
        {
            CoordinateSystem = ProjectedCoordinateSystem.WebMercator;
        }

        public override Vector GetRelativeScale(Location location)
        {
            var k = 1d / Math.Cos(location.Latitude * Math.PI / 180d); // p.44 (7-3)

            return new Vector(k, k);
        }
    }
}
