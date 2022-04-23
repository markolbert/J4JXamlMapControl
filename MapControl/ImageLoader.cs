// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace J4JSoftware.XamlMapControl;

public static partial class ImageLoader
{
    /// <summary>
    /// The System.Net.Http.HttpClient instance used to download images via a http or https Uri.
    /// </summary>
    public static HttpClient HttpClient { get; set; } = new() { Timeout = TimeSpan.FromSeconds(30) };
    
    public static async Task<ImageSource?> LoadImageAsync(Uri uri)
    {
        ImageSource? image = null;

        try
        {
            if (!uri.IsAbsoluteUri || uri.IsFile)
                image = await LoadImageAsync(uri.IsAbsoluteUri ? uri.LocalPath : uri.OriginalString);
            else if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    var response = await GetHttpResponseAsync(uri);

                    if (response != null && response.Buffer != null)
                        image = await LoadImageAsync(response.Buffer);
                }
                else image = new BitmapImage(uri);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ImageLoader: {uri}: {ex.Message}");
        }

        return image;
    }

    internal static async Task<HttpResponse?> GetHttpResponseAsync(Uri uri)
    {
        HttpResponse? response = null;

        try
        {
            using var responseMessage = await HttpClient.GetAsync( uri, HttpCompletionOption.ResponseHeadersRead )
                                                        .ConfigureAwait( false );

            if( responseMessage.IsSuccessStatusCode )
            {
                if( !responseMessage.Headers.TryGetValues( "X-VE-Tile-Info", out IEnumerable<string>? tileInfo )
                || !tileInfo.Contains( "no-tile" ) )
                {
                    var buffer = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait( false );
                    response = new HttpResponse( buffer, responseMessage.Headers.CacheControl?.MaxAge );
                }
            }
            else
                Debug.WriteLine(
                    $"ImageLoader: {uri}: {(int) responseMessage.StatusCode} {responseMessage.ReasonPhrase}" );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ImageLoader: {uri}: {ex.Message}");
        }

        return response;
    }

    public static async Task<ImageSource?> LoadImageAsync(IRandomAccessStream stream)
    {
        var image = new BitmapImage();

        await image.SetSourceAsync(stream);

        return image;
    }

    public static Task<ImageSource?> LoadImageAsync(Stream stream)
    {
        return LoadImageAsync(stream.AsRandomAccessStream());
    }

    public static Task<ImageSource?> LoadImageAsync(byte[] buffer)
    {
        using var stream = new MemoryStream(buffer);

        return LoadImageAsync(stream);
    }

    public static async Task<ImageSource?> LoadImageAsync(string path)
    {
        ImageSource? image = null;

        if( !File.Exists( path ) )
            return image;

        var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(path));

        using var stream = await file.OpenReadAsync();
        image = await LoadImageAsync(stream);

        return image;
    }
}