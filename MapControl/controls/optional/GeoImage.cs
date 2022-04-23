// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace J4JSoftware.XamlMapControl;

public class GeoImage : ContentControl
{
    private const string PixelScaleQuery = "/ifd/{ushort=33550}";
    private const string TiePointQuery = "/ifd/{ushort=33922}";
    private const string TransformQuery = "/ifd/{ushort=34264}";
    private const string NoDataQuery = "/ifd/{ushort=42113}";

    #region SourcePath property

    public static readonly DependencyProperty SourcePathProperty = DependencyProperty.Register(
        nameof(SourcePath), typeof(string), typeof(GeoImage),
        new PropertyMetadata(null, async (o, e) => await ((GeoImage)o).SourcePathPropertyChanged((string)e.NewValue)));

    public string SourcePath
    {
        get => (string)GetValue(SourcePathProperty);
        set => SetValue(SourcePathProperty, value);
    }

    #endregion

    public static async Task<Tuple<BitmapSource?, Matrix>> ReadGeoTiff(string sourcePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(sourcePath));

        using var stream = await file.OpenReadAsync();
        Matrix transform;

        var decoder = await BitmapDecoder.CreateAsync(stream);

        using var swBmp = await decoder.GetSoftwareBitmapAsync();

        var bitmap = new WriteableBitmap(swBmp.PixelWidth, swBmp.PixelHeight);
        swBmp.CopyToBuffer(bitmap.PixelBuffer);

        var query = new List<string>
        {
            PixelScaleQuery, TiePointQuery, TransformQuery, NoDataQuery
        };

        var metadata = await decoder.BitmapProperties.GetPropertiesAsync(query);

        if (metadata.TryGetValue(PixelScaleQuery, out BitmapTypedValue pixelScaleValue) &&
            pixelScaleValue.Value is double[] pixelScale && pixelScale.Length == 3 &&
            metadata.TryGetValue(TiePointQuery, out BitmapTypedValue tiePointValue) &&
            tiePointValue.Value is double[] tiePoint && tiePoint.Length >= 6)
        {
            transform = new Matrix(pixelScale[0], 0d, 0d, -pixelScale[1], tiePoint[3], tiePoint[4]);
        }
        else if (metadata.TryGetValue(TransformQuery, out BitmapTypedValue tformValue) &&
                 tformValue.Value is double[] tform && tform.Length == 16)
            {
                transform = new Matrix(tform[0], tform[1], tform[4], tform[5], tform[3], tform[7]);
            }
            else
            {
                throw new ArgumentException($"No coordinate transformation found in {sourcePath}.");
            }

        return new Tuple<BitmapSource?, Matrix>(bitmap, transform);
    }

    public GeoImage()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
    }

    private async Task SourcePathPropertyChanged( string? sourcePath )
    {
        if( sourcePath == null )
            return;

        var dir = Path.GetDirectoryName(sourcePath);
        if( string.IsNullOrEmpty( dir ) )
            return;

        Tuple<BitmapSource?, Matrix>? geoBitmap = null;

        var ext = Path.GetExtension( sourcePath );

        if( ext.Length >= 4 )
        {
            var file = Path.GetFileNameWithoutExtension( sourcePath );
            var worldFilePath = Path.Combine( dir, file + ext.Remove( 2, 1 ) + "w" );

            if( File.Exists( worldFilePath ) )
                geoBitmap = await ReadWorldFileImage( sourcePath, worldFilePath );
        }

        geoBitmap ??= await ReadGeoTiff( sourcePath );
        if( geoBitmap.Item1 == null )
            return;

        var bitmap = geoBitmap.Item1;
        var transform = geoBitmap.Item2;

        var image = new Image { Source = bitmap, Stretch = Stretch.Fill };

        Content = image;

        if( transform.M12 != 0 || transform.M21 != 0 )
        {
            var rotation = ( Math.Atan2( transform.M12, transform.M11 ) + Math.Atan2( transform.M21, -transform.M22 ) )
              * 90d
              / Math.PI;

            image.RenderTransform = new RotateTransform { Angle = -rotation };

            // effective unrotated transform
            transform.M11 = Math.Sqrt( transform.M11 * transform.M11 + transform.M12 * transform.M12 );
            transform.M22 = -Math.Sqrt( transform.M22 * transform.M22 + transform.M21 * transform.M21 );
            transform.M12 = 0;
            transform.M21 = 0;
        }

        var rect = new Rect( transform.Transform( new Point() ),
                             transform.Transform( new Point( bitmap.PixelWidth, bitmap.PixelHeight ) ) );

        var boundingBox = new BoundingBox( rect.Y, rect.X, rect.Y + rect.Height, rect.X + rect.Width );
        MapPanel.SetBoundingBox( this, boundingBox );
    }

    private static async Task<Tuple<BitmapSource?, Matrix>> ReadWorldFileImage(string sourcePath, string worldFilePath)
    {
        var bitmap = await ImageLoader.LoadImageAsync(sourcePath) as BitmapSource;

        var transform = await Task.Run(() =>
        {
            var parameters = File.ReadLines(worldFilePath)
                                 .Take(6)
                                 .Select((line, i) =>
                                  {
                                      if (!double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out double parameter))
                                      {
                                          throw new ArgumentException($"Failed parsing line {i + 1} in world file {worldFilePath}.");
                                      }
                                      return parameter;
                                  })
                                 .ToList();

            if (parameters.Count != 6)
                throw new ArgumentException($"Insufficient number of parameters in world file {worldFilePath}.");

            return new Matrix(
                parameters[0],  // line 1: A or M11
                parameters[1],  // line 2: D or M12
                parameters[2],  // line 3: B or M21
                parameters[3],  // line 4: E or M22
                parameters[4],  // line 5: C or OffsetX
                parameters[5]); // line 6: F or OffsetY
        });

        return new Tuple<BitmapSource?, Matrix>(bitmap, transform);
    }
}