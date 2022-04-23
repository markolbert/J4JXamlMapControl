// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using J4JSoftware.XamlMapControl.Projections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Displays a single map image from a Web Map Service (WMS).
/// </summary>
public class WmsImageLayer : MapImageLayer
{
    #region ServiceUri property

    /// <summary>
    /// The base request URL. 
    /// </summary>
    public static readonly DependencyProperty ServiceUriProperty = DependencyProperty.Register( nameof( ServiceUri ),
        typeof( Uri ),
        typeof( WmsImageLayer ),
        new PropertyMetadata( null, async ( o, _ ) => await ( (WmsImageLayer) o ).UpdateImageAsync() ) );

    public Uri? ServiceUri
    {
        get => (Uri)GetValue(ServiceUriProperty);
        set => SetValue(ServiceUriProperty, value);
    }

    #endregion

    #region Layers property

    /// <summary>
    /// Comma-separated list of Layer names to be displayed. If not set, the first Layer is displayed.
    /// </summary>
    public static readonly DependencyProperty LayersProperty = DependencyProperty.Register( 
        nameof( Layers ),
        typeof( string ),
        typeof( WmsImageLayer ),
        new PropertyMetadata( null, async ( o, _ ) => await ( (WmsImageLayer) o ).UpdateImageAsync() ) );

    public string? Layers
    {
        get => (string)GetValue(LayersProperty);
        set => SetValue(LayersProperty, value);
    }

    #endregion

    #region Styles property

    /// <summary>
    /// Comma-separated list of requested styles. Default is an empty string.
    /// </summary>
    public static readonly DependencyProperty StylesProperty = DependencyProperty.Register( 
        nameof( Styles ),
        typeof( string ),
        typeof( WmsImageLayer ),
        new PropertyMetadata( string.Empty, async ( o, _ ) => await ( (WmsImageLayer) o ).UpdateImageAsync() ) );

    public string? Styles
    {
        get => (string?)GetValue(StylesProperty);
        set => SetValue(StylesProperty, value);
    }

    #endregion

    public WmsImageLayer()
    {
        foreach( var child in Children
                             .Where( x => x is FrameworkElement )
                             .Cast<FrameworkElement>() )
        {
            child.UseLayoutRounding = true;
        }
    }

    /// <summary>
    /// Gets a list of all layer names returned by a GetCapabilities response.
    /// </summary>
    public async Task<IEnumerable<string>?> GetLayerNamesAsync()
    {
        var capabilities = await GetCapabilitiesAsync();

        if( capabilities == null )
            return null;

        var ns = capabilities.Name.Namespace;

        return capabilities
                   .Descendants( ns + "Layer" )
                   .Select( e => e.Element( ns + "Name" )?.Value )
                   .Where( x => !string.IsNullOrEmpty( x ) )
                   .Cast<string>();
    }

    /// <summary>
    /// Loads an XElement from the URL returned by GetCapabilitiesRequestUri().
    /// </summary>
    public async Task<XElement?> GetCapabilitiesAsync()
    {
        if( ServiceUri == null )
            return null;

        var uri = GetCapabilitiesRequestUri();

        if( string.IsNullOrEmpty( uri ) )
            return null;

        try
        {
            await using var stream = await ImageLoader.HttpClient.GetStreamAsync(uri);

            return XDocument.Load(stream).Root;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WmsImageLayer: {uri}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Loads an XElement from the URL returned by GetFeatureInfoRequestUri().
    /// </summary>
    public async Task<XElement?> GetFeatureInfoAsync(Point position)
    {
        if( ServiceUri == null )
            return null;

        var uri = GetFeatureInfoRequestUri(position, "text/xml");

        if( string.IsNullOrEmpty( uri ) )
            return null;

        try
        {
            await using var stream = await ImageLoader.HttpClient.GetStreamAsync(uri);

            return XDocument.Load(stream).Root;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WmsImageLayer: {uri}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Gets a response string from the URL returned by GetFeatureInfoRequestUri().
    /// </summary>
    public async Task<string?> GetFeatureInfoTextAsync(Point position, string format = "text/plain")
    {
        if( ServiceUri == null )
            return null;

        var uri = GetFeatureInfoRequestUri(position, format);

        if( string.IsNullOrEmpty( uri ) )
            return null;

        try
        {
            return await ImageLoader.HttpClient.GetStringAsync(uri);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WmsImageLayer: {uri}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Loads an ImageSource from the URL returned by GetMapRequestUri().
    /// </summary>
    protected override async Task<ImageSource?> GetImageAsync()
    {
        if( ServiceUri == null )
            return null;

        if (Layers == null &&
            ServiceUri.ToString().IndexOf("LAYERS=", StringComparison.OrdinalIgnoreCase) < 0)
        {
            Layers = (await GetLayerNamesAsync())?.FirstOrDefault() ?? ""; // get first Layer from Capabilities
        }

        var uri = GetMapRequestUri();

        return !string.IsNullOrEmpty( uri ) 
            ? await ImageLoader.LoadImageAsync( new Uri( uri ) ) 
            : null;
    }

    /// <summary>
    /// Returns a GetCapabilities request URL string.
    /// </summary>
    protected virtual string? GetCapabilitiesRequestUri()
    {
        var retVal = GetRequestUri( "GetCapabilities" );

        return string.IsNullOrEmpty( retVal ) ? null : retVal.Replace( " ", "%20" );
    }

    /// <summary>
    /// Returns a GetMap request URL string.
    /// </summary>
    protected virtual string? GetMapRequestUri()
    {
        if( ParentMap?.MapProjection == null || BoundingBox == null )
            return null;

        var projection = ParentMap.MapProjection!;

        var uri = GetRequestUri( "GetMap" );
        if( string.IsNullOrEmpty( uri ) )
            return null;

        if( uri.IndexOf( "LAYERS=", StringComparison.OrdinalIgnoreCase ) < 0 && Layers != null )
            uri += "&LAYERS=" + Layers;

        if( uri.IndexOf( "STYLES=", StringComparison.OrdinalIgnoreCase ) < 0 && Styles != null )
            uri += "&STYLES=" + Styles;

        if( uri.IndexOf( "FORMAT=", StringComparison.OrdinalIgnoreCase ) < 0 )
            uri += "&FORMAT=image/png";

        var mapRect = projection.BoundingBoxToRect( BoundingBox );
        var viewScale = ParentMap.ViewTransform.Scale;

        uri += "&" + GetCrsParam( projection );
        uri += "&" + GetBboxParam( projection, mapRect );
        uri += "&WIDTH=" + (int) Math.Round( viewScale * mapRect.Width );
        uri += "&HEIGHT=" + (int) Math.Round( viewScale * mapRect.Height );

        uri = uri.Replace( " ", "%20" );

        return uri;
    }

    /// <summary>
    /// Returns a GetFeatureInfo request URL string.
    /// </summary>
    protected virtual string? GetFeatureInfoRequestUri( Point position, string format )
    {
        if( ParentMap?.MapProjection == null || BoundingBox == null )
            return null;

        var projection = ParentMap.MapProjection!;

        var uri = GetRequestUri( "GetFeatureInfo" );
        if( string.IsNullOrEmpty( uri ) )
            return null;

        var i = uri.IndexOf( "LAYERS=", StringComparison.OrdinalIgnoreCase );

        if( i >= 0 )
        {
            i += 7;
            var j = uri.IndexOf( '&', i );
            var layers = j >= i ? uri.Substring( i, j - i ) : uri.Substring( i );
            uri += "&QUERY_LAYERS=" + layers;
        }
        else
            if( Layers != null )
            {
                uri += "&LAYERS=" + Layers;
                uri += "&QUERY_LAYERS=" + Layers;
            }

        var mapRect = projection.BoundingBoxToRect( BoundingBox );
        var viewRect = GetViewRect( mapRect );
        var viewSize = ParentMap.RenderSize;

        var transform = new Matrix( 1, 0, 0, 1, -viewSize.Width / 2, -viewSize.Height / 2 );
        transform.Rotate( -viewRect.Rotation );
        transform.Translate( viewRect.Width / 2, viewRect.Height / 2 );

        var imagePos = transform.Transform( position );

        uri += "&" + GetCrsParam( projection );
        uri += "&" + GetBboxParam( projection, mapRect );
        uri += "&WIDTH=" + (int) Math.Round( viewRect.Width );
        uri += "&HEIGHT=" + (int) Math.Round( viewRect.Height );
        uri += "&I=" + (int) Math.Round( imagePos.X );
        uri += "&J=" + (int) Math.Round( imagePos.Y );
        uri += "&INFO_FORMAT=" + format;

        uri = uri.Replace( " ", "%20" );

        return uri;
    }

    protected virtual string GetCrsParam( MapProjection projection ) => "CRS=" + projection.GetCrsValue();

    protected virtual string GetBboxParam( MapProjection projection, Rect mapRect ) =>
        "BBOX=" + projection.GetBboxValue( mapRect );

    protected string? GetRequestUri(string request)
    {
        if( ServiceUri == null )
            return null;

        var uri = ServiceUri.ToString();

        if (!uri.EndsWith("?") && !uri.EndsWith("&"))
            uri += !uri.Contains('?') ? "?" : "&";

        if (uri.IndexOf("SERVICE=", StringComparison.OrdinalIgnoreCase) < 0)
            uri += "SERVICE=WMS&";

        if (uri.IndexOf("VERSION=", StringComparison.OrdinalIgnoreCase) < 0)
            uri += "VERSION=1.3.0&";

        return uri + "REQUEST=" + request;
    }
}