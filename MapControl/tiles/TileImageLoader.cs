// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Loads and optionally caches map tile images for a MapTileLayer.
/// </summary>
public class TileImageLoader : ITileImageLoader
{
    /// <summary>
    /// Default folder path where an IImageCache instance may save cached data, i.e. C:\ProgramData\MapControl\TileCache
    /// </summary>
    public static string DefaultCacheFolder =>
        Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData ),
                      "MapControl",
                      "TileCache" );

    /// <summary>
    /// Maximum number of parallel tile loading tasks. The default value is 4.
    /// </summary>
    public static int MaxLoadTasks { get; set; } = 4;

    /// <summary>
    /// Default expiration time for cached tile images. Used when no expiration time
    /// was transmitted on download. The default value is one day.
    /// </summary>
    public static TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Maximum expiration time for cached tile images. A transmitted expiration time
    /// that exceeds this value is ignored. The default value is ten days.
    /// </summary>
    public static TimeSpan MaxCacheExpiration { get; set; } = TimeSpan.FromDays(10);

    /// <summary>
    /// An IImageCache implementation used to cache tile images.
    /// </summary>
    public static Caching.IImageCache? Cache { get; set; }

    private ConcurrentStack<Tile> _pendingTiles = new();

    /// <summary>
    /// The current TileSource, passed to the most recent LoadTiles call.
    /// </summary>
    public TileSource? TileSource { get; private set; }

    /// <summary>
    /// Loads all pending tiles from the tiles collection.
    /// If tileSource.UriFormat starts with "http" and cacheName is a non-empty string,
    /// tile images will be cached in the TileImageLoader's Cache - if that is not null.
    /// </summary>
    public Task LoadTiles(IEnumerable<Tile> tiles, TileSource? tileSource, string? cacheName)
    {
        _pendingTiles?.Clear(); // stop processing the current queue

        TileSource = tileSource;

        if( tileSource == null )
            return Task.CompletedTask;

        _pendingTiles = new ConcurrentStack<Tile>(tiles.Where(tile => tile.Pending).Reverse());

        var numTasks = Math.Min(_pendingTiles.Count, MaxLoadTasks);

        if( numTasks <= 0 )
            return Task.CompletedTask;

        if (Cache == null || tileSource.UriFormat == null || !tileSource.UriFormat.StartsWith("http"))
            cacheName = null; // no tile caching

        var tasks = Enumerable.Range(0, numTasks)
                              .Select(_ => Task.Run(() => LoadPendingTiles(_pendingTiles, tileSource, cacheName)));

        return Task.WhenAll(tasks);
    }

    private async Task LoadPendingTiles( ConcurrentStack<Tile> pendingTiles, TileSource tileSource, string? cacheName )
    {
        while( pendingTiles.TryPop( out var tile ) )
        {
            tile.Pending = false;

            try
            {
                await LoadTile( tile, tileSource, cacheName ).ConfigureAwait( false );
            }
            catch( Exception ex )
            {
                Debug.WriteLine( $"TileImageLoader: {tile.ZoomLevel}/{tile.XIndex}/{tile.Y}: {ex.Message}" );
            }
        }
    }

    private Task LoadTile(Tile tile, TileSource tileSource, string? cacheName)
    {
        if (string.IsNullOrEmpty(cacheName))
            return LoadUncachedTile(tile, tileSource);

        var uri = tileSource.GetUri(tile.XIndex, tile.Y, tile.ZoomLevel);

        if( uri == null )
            return Task.CompletedTask;

        var extension = Path.GetExtension(uri.LocalPath);

        if (string.IsNullOrEmpty(extension) || extension == ".jpeg")
            extension = ".jpg";

        var cacheKey = string.Format(CultureInfo.InvariantCulture,
                                     "{0}/{1}/{2}/{3}{4}", cacheName, tile.ZoomLevel, tile.XIndex, tile.Y, extension);

        return LoadCachedTile(tile, uri, cacheKey);
    }

    private async Task LoadCachedTile(Tile tile, Uri uri, string cacheKey)
    {
        var cacheItem = await Cache!.GetAsync(cacheKey).ConfigureAwait(false);
        var buffer = cacheItem?.Item1;

        if (cacheItem == null || cacheItem.Item2 < DateTime.UtcNow)
        {
            var response = await ImageLoader.GetHttpResponseAsync(uri).ConfigureAwait(false);

            if (response != null) // download succeeded
            {
                buffer = response.Buffer; // may be null or empty when no tile available, but still be cached

                if( buffer != null)
                    await Cache.SetAsync(cacheKey, buffer, GetExpiration(response.MaxAge)).ConfigureAwait(false);
            }
        }
        //else System.Diagnostics.Debug.WriteLine($"Cached: {cacheKey}");

        if (buffer != null && buffer.Length > 0)
            await SetTileImage(tile, () => ImageLoader.LoadImageAsync(buffer)).ConfigureAwait(false);
    }

    private Task LoadUncachedTile( Tile tile, TileSource tileSource ) =>
        SetTileImage( tile, () => tileSource.LoadImageAsync( tile.XIndex, tile.Y, tile.ZoomLevel ) );

    private static Task SetTileImage(Tile tile, Func<Task<ImageSource?>> loadImageFunc)
    {
        var tcs = new TaskCompletionSource();

        if( tile.Image.DispatcherQueue.TryEnqueue( DispatcherQueuePriority.Low, callback ) )
            return tcs.Task;

        tile.Pending = true;
        tcs.TrySetResult();

        return tcs.Task;

        // ReSharper disable once InconsistentNaming
        async void callback()
        {
            try
            {
                tile.SetImage(await loadImageFunc());
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

    }

    private DateTime GetExpiration(TimeSpan? maxAge)
    {
        switch( maxAge )
        {
            case null:
                maxAge = DefaultCacheExpiration;
                break;

            default:
            {
                if (maxAge.Value > MaxCacheExpiration)
                    maxAge = MaxCacheExpiration;
                break;
            }
        }

        return DateTime.UtcNow.Add(maxAge.Value);
    }
}