// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Collections.Generic;

namespace J4JSoftware.XamlMapControl.Projections;

public class GeoApiProjectionFactory : MapProjectionFactory
{
    public const int WorldMercator = 3395;
    public const int WebMercator = 3857;
    public const int AutoUtm = 42001;
    public const int Ed50UtmFirst = 23028;
    public const int Ed50UtmLast = 23038;
    public const int Etrs89UtmFirst = 25828;
    public const int Etrs89UtmLast = 25838;
    public const int Wgs84UtmNorthFirst = 32601;
    public const int Wgs84UtmNorthLast = 32660;
    public const int Wgs84UpsNorth = 32661;
    public const int Wgs84UtmSouthFirst = 32701;
    public const int Wgs84UtmSouthLast = 32760;
    public const int Wgs84UpsSouth = 32761;

    public Dictionary<int, string> CoordinateSystemWkts { get; } = new Dictionary<int, string>();

    public override MapProjection? GetProjection(string crsId)
    {
        MapProjection? projection = null;

        var str = crsId.StartsWith("EPSG:") ? crsId.Substring(5)
            : crsId.StartsWith("AUTO2:") ? crsId.Substring(6)
            : null;

        if( !int.TryParse( str, out int code ) )
            return base.GetProjection( crsId );

        if (CoordinateSystemWkts.TryGetValue(code, out string? wkt))
            projection = new GeoApiProjection(wkt);
        else
        {
            switch (code)
            {
                case WorldMercator:
                    projection = new WorldMercatorProjectionNG();
                    break;

                case WebMercator:
                    projection = new WebMercatorProjectionNG();
                    break;

                case AutoUtm:
                    projection = new AutoUtmProjection();
                    break;

                case <= Ed50UtmLast and >= Ed50UtmFirst:
                    projection = new Ed50UtmProjection(code % 100);
                    break;

                case <= Etrs89UtmLast and >= Etrs89UtmFirst:
                    projection = new Etrs89UtmProjection(code % 100);
                    break;

                case <= Wgs84UtmNorthLast and >= Wgs84UtmNorthFirst:
                    projection = new Wgs84UtmProjection(code % 100, true);
                    break;

                case <= Wgs84UtmSouthLast and >= Wgs84UtmSouthFirst:
                    projection = new Wgs84UtmProjection(code % 100, false);
                    break;

                case Wgs84UpsNorth:
                    projection = new UpsNorthProjection();
                    break;

                case Wgs84UpsSouth:
                    projection = new UpsSouthProjection();
                    break;
            }
        }

        return projection ?? base.GetProjection(crsId);
    }
}