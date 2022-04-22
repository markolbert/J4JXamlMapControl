using System;

namespace J4JSoftware.XamlMapControl;

public class TmsTileSource : TileSource
{
    public override Uri? GetUri( int x, int y, int zoomLevel ) =>
        base.GetUri( x, ( 1 << zoomLevel ) - 1 - y, zoomLevel );
}
