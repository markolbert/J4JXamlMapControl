// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Provides the download Uri or ImageSource of map tiles.
/// </summary>
[TypeConverter(typeof(TileSourceConverter))]
public class TileSource
{
    private string? _uriFormat;

    /// <summary>
    /// Gets or sets the format string to produce tile request Uris.
    /// </summary>
    public string? UriFormat
    {
        get => _uriFormat;

        set
        {
            _uriFormat = value?.Replace("{c}", "{s}"); // for backwards compatibility since 5.4.0

            if (Subdomains == null && _uriFormat != null && _uriFormat.Contains("{s}"))
                Subdomains = new[] { "a", "b", "c" }; // default OpenStreetMap subdomains
        }
    }

    /// <summary>
    /// Gets or sets an array of request subdomain names that are replaced for the {s} format specifier.
    /// </summary>
    public string[]? Subdomains { get; set; }

    /// <summary>
    /// Gets the image Uri for the specified tile indices and zoom level.
    /// </summary>
    public virtual Uri? GetUri(int x, int y, int zoomLevel)
    {
        Uri? uri = null;

        if( UriFormat == null )
            return uri;

        var uriString = UriFormat
                       .Replace("{x}", x.ToString())
                       .Replace("{y}", y.ToString())
                       .Replace("{z}", zoomLevel.ToString());

        if (Subdomains is { Length: > 0 })
            uriString = uriString.Replace("{s}", Subdomains[(x + y) % Subdomains.Length]);

        uri = new Uri(uriString, UriKind.RelativeOrAbsolute);

        return uri;
    }

    /// <summary>
    /// Loads a tile ImageSource asynchronously from GetUri(x, y, zoomLevel).
    /// </summary>
    public virtual Task<ImageSource?> LoadImageAsync(int x, int y, int zoomLevel)
    {
        var uri = GetUri(x, y, zoomLevel);

        return uri != null ? ImageLoader.LoadImageAsync(uri) : Task.FromResult((ImageSource?)null);
    }
}