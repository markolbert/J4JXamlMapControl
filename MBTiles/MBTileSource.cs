// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl.MBTiles;

public class MBTileSource : TileSource, IDisposable
{
    public MbTileData? TileData { get; }

    public MBTileSource(MbTileData tiledata)
    {
        var format = tiledata.Metadata["format"];

        if (format == "png" || format == "jpg")
            TileData = tiledata;
        else Debug.WriteLine($"MBTileSource: unsupported format '{format}'");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && TileData != null)
        {
            TileData.Dispose();
        }
    }

    public override async Task<ImageSource?> LoadImageAsync(int x, int y, int zoomLevel)
    {
        ImageSource? image = null;

        if( TileData == null )
            return image;

        var buffer = await TileData.ReadImageBufferAsync(x, y, zoomLevel);

        if (buffer != null)
        {
            try
            {
                image = await ImageLoader.LoadImageAsync(buffer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MBTileSource : {ex.Message}");
            }
        }

        return image;
    }
}