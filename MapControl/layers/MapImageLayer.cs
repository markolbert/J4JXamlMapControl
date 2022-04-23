// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Displays a single map image, e.g. from a Web Map Service (WMS).
/// The image must be provided by the abstract GetImageAsync() method.
/// </summary>
public abstract class MapImageLayer : MapPanel, IMapLayer
{
    #region Description property

    /// <summary>
    /// Description of the layer. Used to display copyright information on top of the map.
    /// </summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(MapImageLayer), new PropertyMetadata(null));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    #endregion

    #region RelativeImageSize property

    /// <summary>
    /// Relative size of the map image in relation to the current view size.
    /// Setting a value greater than one will let MapImageLayer request images that
    /// are larger than the view, in order to support smooth panning.
    /// </summary>
    public static readonly DependencyProperty RelativeImageSizeProperty = DependencyProperty.Register(
        nameof(RelativeImageSize), typeof(double), typeof(MapImageLayer), new PropertyMetadata(1d));

    public double RelativeImageSize
    {
        get => (double)GetValue(RelativeImageSizeProperty);
        set => SetValue(RelativeImageSizeProperty, value);
    }

    #endregion

    #region UpdateInterval property

    /// <summary>
    /// Minimum time interval between images updates.
    /// </summary>
    public static readonly DependencyProperty UpdateIntervalProperty = DependencyProperty.Register(
        nameof( UpdateInterval ),
        typeof( TimeSpan ),
        typeof( MapImageLayer ),
        new PropertyMetadata( TimeSpan.FromSeconds( 0.2 ),
                              ( o, e ) => ( (MapImageLayer) o )._updateTimer.Interval = (TimeSpan) e.NewValue ) );

    public TimeSpan UpdateInterval
    {
        get => (TimeSpan)GetValue(UpdateIntervalProperty);
        set => SetValue(UpdateIntervalProperty, value);
    }

    #endregion

    #region UpdateWhileViewportChanging property

    /// <summary>
    /// Controls if images are updated while the viewport is still changing.
    /// </summary>
    public static readonly DependencyProperty UpdateWhileViewportChangingProperty = DependencyProperty.Register(
        nameof( UpdateWhileViewportChanging ),
        typeof( bool ),
        typeof( MapImageLayer ),
        new PropertyMetadata( false ) );

    public bool UpdateWhileViewportChanging
    {
        get => (bool)GetValue(UpdateWhileViewportChangingProperty);
        set => SetValue(UpdateWhileViewportChangingProperty, value);
    }

    #endregion

    #region MapBackground property

    /// <summary>
    /// Optional background brush. Sets MapBase.Background if not null and this layer is the base map layer.
    /// </summary>
    public static readonly DependencyProperty MapBackgroundProperty = DependencyProperty.Register(
        nameof( MapBackground ),
        typeof( Brush ),
        typeof( MapImageLayer ),
        new PropertyMetadata( null ) );

    public Brush MapBackground
    {
        get => (Brush)GetValue(MapBackgroundProperty);
        set => SetValue(MapBackgroundProperty, value);
    }

    #endregion

    #region MapForeground property

    /// <summary>
    /// Optional foreground brush. Sets MapBase.Foreground if not null and this layer is the base map layer.
    /// </summary>
    public static readonly DependencyProperty MapForegroundProperty = DependencyProperty.Register(
        nameof( MapForeground ),
        typeof( Brush ),
        typeof( MapImageLayer ),
        new PropertyMetadata( null ) );

    public Brush MapForeground
    {
        get => (Brush)GetValue(MapForegroundProperty);
        set => SetValue(MapForegroundProperty, value);
    }

    #endregion

    private readonly DispatcherQueueTimer _updateTimer;
    private bool _updateInProgress;

    protected MapImageLayer()
    {
        _updateTimer = this.CreateTimer(UpdateInterval);
        _updateTimer.Tick += async (_, _) => await UpdateImageAsync();
    }

    /// <summary>
    /// The current BoundingBox
    /// </summary>
    public BoundingBox? BoundingBox { get; private set; }

    protected abstract Task<ImageSource?> GetImageAsync();

    protected override void SetParentMap(MapBase? map)
    {
        if (map == null)
        {
            _updateTimer.Stop();
            ClearImages();
            Children.Clear();
        }
        else if (Children.Count == 0)
        {
            Children.Add(new Image { Opacity = 0d, Stretch = Stretch.Fill });
            Children.Add(new Image { Opacity = 0d, Stretch = Stretch.Fill });
        }

        base.SetParentMap(map);
    }

    protected override async void OnViewportChanged(ViewportChangedEventArgs e)
    {
        if (e.ProjectionChanged)
        {
            ClearImages();

            base.OnViewportChanged(e);

            await UpdateImageAsync();
        }
        else
        {
            AdjustBoundingBox(e.LongitudeOffset);

            base.OnViewportChanged(e);

            _updateTimer.Run(!UpdateWhileViewportChanging);
        }
    }

    protected async Task UpdateImageAsync()
    {
        _updateTimer.Stop();

        if (_updateInProgress)
        {
            _updateTimer.Run(); // update image on next timer tick
        }
        else if (ParentMap != null && ParentMap.RenderSize.Width > 0 && ParentMap.RenderSize.Height > 0)
        {
            _updateInProgress = true;

            UpdateBoundingBox();

            ImageSource? image = null;

            if (BoundingBox != null)
            {
                try
                {
                    image = await GetImageAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MapImageLayer: {ex.Message}");
                }
            }

            SwapImages(image);

            _updateInProgress = false;
        }
    }

    private void UpdateBoundingBox()
    {
        var width = ParentMap!.RenderSize.Width * RelativeImageSize;
        var height = ParentMap.RenderSize.Height * RelativeImageSize;
        var x = (ParentMap.RenderSize.Width - width) / 2d;
        var y = (ParentMap.RenderSize.Height - height) / 2d;
        var rect = new Rect(x, y, width, height);

        BoundingBox = ParentMap.ViewRectToBoundingBox(rect);
    }

    private void AdjustBoundingBox(double longitudeOffset)
    {
        if( !( Math.Abs( longitudeOffset ) > 180d ) || BoundingBox == null )
            return;

        var offset = 360d * Math.Sign(longitudeOffset);

        BoundingBox = new BoundingBox(BoundingBox, offset);

        foreach (var image in Children.OfType<Image>())
        {
            var imageBoundingBox = GetBoundingBox(image);

            if (imageBoundingBox != null)
            {
                SetBoundingBox(image, new BoundingBox(imageBoundingBox, offset));
            }
        }
    }

    private void ClearImages()
    {
        foreach (var image in Children.OfType<Image>())
        {
            image.ClearValue(BoundingBoxProperty);
            image.ClearValue(Image.SourceProperty);
        }
    }

    private void SwapImages(ImageSource? image)
    {
        if( Children.Count < 2 )
            return;

        var topImage = (Image)Children[0];
        var bottomImage = (Image)Children[1];

        Children.RemoveAt(0);
        Children.Insert(1, topImage);

        topImage.Source = image;
        SetBoundingBox(topImage, BoundingBox);

        topImage.BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            To = 1d,
            Duration = MapBase.ImageFadeDuration
        });

        bottomImage.BeginAnimation(OpacityProperty, new DoubleAnimation
        {
            To = 0d,
            BeginTime = MapBase.ImageFadeDuration,
            Duration = TimeSpan.Zero
        });
    }
}