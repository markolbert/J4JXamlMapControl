// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;

namespace J4JSoftware.XamlMapControl.Projections;

public class MapProjectionFactory
{
    public virtual MapProjection GetProjection(string crsId) =>
        crsId switch
        {
            WorldMercatorProjection.DefaultCrsId => new WorldMercatorProjection(),
            WebMercatorProjection.DefaultCrsId => new WebMercatorProjection(),
            EquirectangularProjection.DefaultCrsId => new EquirectangularProjection(),
            OrthographicProjection.DefaultCrsId => new OrthographicProjection(),
            AutoEquirectangularProjection.DefaultCrsId => new AutoEquirectangularProjection(),
            GnomonicProjection.DefaultCrsId => new GnomonicProjection(),
            StereographicProjection.DefaultCrsId => new StereographicProjection(),
            "EPSG:97003" => // proprietary CRS ID
                new AzimuthalEquidistantProjection( crsId ),

            _ => throw new ArgumentException( $"Unsupported {crsId}" )
        };
}