// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace J4JSoftware.XamlMapControl;

public class Tile
{
    public Tile( int zoomLevel, int x, int y )
    {
        ZoomLevel = zoomLevel;
        X = x;
        Y = y;
    }

    public int ZoomLevel { get; }
    public int X { get; }
    public int Y { get; }

    public int XIndex
    {
        get
        {
            var numTiles = 1 << ZoomLevel;
            return ( ( X % numTiles ) + numTiles ) % numTiles;
        }
    }

    public Image Image { get; } = new() { Opacity = 0d, Stretch = Stretch.Fill };
    public bool Pending { get; set; } = true;

    public void SetImage( ImageSource? image, bool fadeIn = true )
    {
        Pending = false;

        if( image != null && fadeIn && MapBase.ImageFadeDuration > TimeSpan.Zero )
        {
            if( image is BitmapImage bitmap && bitmap.UriSource != null )
            {
                bitmap.ImageOpened += BitmapImageOpened;
                bitmap.ImageFailed += BitmapImageFailed;
            }
            else FadeIn();
        }
        else Image.Opacity = 1d;

        Image.Source = image;
    }

    private void BitmapImageOpened( object sender, RoutedEventArgs e )
    {
        var bitmap = (BitmapImage) sender;

        bitmap.ImageOpened -= BitmapImageOpened;
        bitmap.ImageFailed -= BitmapImageFailed;

        FadeIn();
    }

    private void FadeIn()
    {
        Image.BeginAnimation(UIElement.OpacityProperty,
                             new DoubleAnimation
                             {
                                 From = 0d,
                                 To = 1d,
                                 Duration = MapBase.ImageFadeDuration,
                                 FillBehavior = FillBehavior.Stop
                             });

        Image.Opacity = 1d;
    }

    private void BitmapImageFailed( object sender, ExceptionRoutedEventArgs e )
    {
        var bitmap = (BitmapImage) sender;

        bitmap.ImageOpened -= BitmapImageOpened;
        bitmap.ImageFailed -= BitmapImageFailed;

        Image.Source = null;
    }
}