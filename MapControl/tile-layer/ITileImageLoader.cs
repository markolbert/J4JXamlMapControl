using System.Collections.Generic;
using System.Threading.Tasks;

namespace J4JSoftware.XamlMapControl;

public interface ITileImageLoader
{
    TileSource? TileSource { get; }
    Task LoadTiles(IEnumerable<Tile> tiles, TileSource? tileSource, string cacheName);
}
