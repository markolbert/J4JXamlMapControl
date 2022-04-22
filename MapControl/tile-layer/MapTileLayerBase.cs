// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl;

public abstract class MapTileLayerBase : Panel, IMapLayer
{
    #region TileSource property

    /// <summary>
    /// Provides map tile URIs or images.
    /// </summary>
    public static readonly DependencyProperty TileSourceProperty = DependencyProperty.Register(
        nameof(TileSource), typeof(TileSource), typeof(MapTileLayerBase),
        new PropertyMetadata(null, async (o, e) => await ((MapTileLayerBase)o).Update()));

    public TileSource? TileSource
    {
        get => (TileSource?)GetValue(TileSourceProperty);
        set => SetValue(TileSourceProperty, value);
    }

    #endregion

    #region SourceName property

    /// <summary>
    /// Name of the TileSource. Used as component of a tile cache key.
    /// </summary>
    public static readonly DependencyProperty SourceNameProperty = DependencyProperty.Register(
        nameof(SourceName), typeof(string), typeof(MapTileLayerBase), new PropertyMetadata(null));

    public string SourceName
    {
        get => (string)GetValue(SourceNameProperty);
        set => SetValue(SourceNameProperty, value);
    }

    #endregion

    #region Description

    /// <summary>
    /// Description of the layer. Used to display copyright information on top of the map.
    /// </summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(MapTileLayerBase), new PropertyMetadata(null));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    #endregion

    #region MaxBackgroundLevels property

    /// <summary>
    /// Maximum number of background tile levels. Default value is 8.
    /// Only effective in a MapTileLayer or WmtsTileLayer that is the MapLayer of its ParentMap.
    /// </summary>
    public static readonly DependencyProperty MaxBackgroundLevelsProperty = DependencyProperty.Register(
        nameof(MaxBackgroundLevels), typeof(int), typeof(MapTileLayerBase), new PropertyMetadata(8));

    public int MaxBackgroundLevels
    {
        get => (int)GetValue(MaxBackgroundLevelsProperty);
        set => SetValue(MaxBackgroundLevelsProperty, value);
    }

    #endregion

    #region UpdateInterval property

    /// <summary>
    /// Minimum time interval between tile updates.
    /// </summary>
    public static readonly DependencyProperty UpdateIntervalProperty = DependencyProperty.Register(
        nameof(UpdateInterval), typeof(TimeSpan), typeof(MapTileLayerBase),
        new PropertyMetadata(TimeSpan.FromSeconds(0.2), (o, e) => ((MapTileLayerBase)o)._updateTimer.Interval = (TimeSpan)e.NewValue));

    public TimeSpan UpdateInterval
    {
        get => (TimeSpan)GetValue(UpdateIntervalProperty);
        set => SetValue(UpdateIntervalProperty, value);
    }

    #endregion

    #region UpdateWhileViewportChanging property

    /// <summary>
    /// Controls if tiles are updated while the viewport is still changing.
    /// </summary>
    public static readonly DependencyProperty UpdateWhileViewportChangingProperty = DependencyProperty.Register(
        nameof(UpdateWhileViewportChanging), typeof(bool), typeof(MapTileLayerBase), new PropertyMetadata(false));

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
        nameof(MapBackground), typeof(Brush), typeof(MapTileLayerBase), new PropertyMetadata(null));

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
        nameof(MapForeground), typeof(Brush), typeof(MapTileLayerBase), new PropertyMetadata(null));

    public Brush MapForeground
    {
        get => (Brush)GetValue(MapForegroundProperty);
        set => SetValue(MapForegroundProperty, value);
    }

    #endregion

    private readonly DispatcherQueueTimer _updateTimer;

    private MapBase? _parentMap;

    protected MapTileLayerBase(ITileImageLoader tileImageLoader)
    {
        RenderTransform = new MatrixTransform();
        TileImageLoader = tileImageLoader;

        _updateTimer = this.CreateTimer(UpdateInterval);
        _updateTimer.Tick += async (s, e) => await Update();

        MapPanel.InitMapElement(this);
    }

    public ITileImageLoader TileImageLoader { get; }

    public MapBase? ParentMap
    {
        get => _parentMap;

        set
        {
            if (_parentMap != null)
                _parentMap.ViewportChanged -= OnViewportChanged;

            _parentMap = value;

            if (_parentMap != null)
                _parentMap.ViewportChanged += OnViewportChanged;

            _updateTimer.Run();
        }
    }

    protected bool IsBaseMapLayer =>
        _parentMap != null
     && _parentMap.Children.Count > 0
     && _parentMap.Children[0] == this;

    protected abstract void SetRenderTransform();
    protected abstract Task UpdateTileLayer();

    private Task Update()
    {
        _updateTimer.Stop();

        return UpdateTileLayer();
    }

    private async void OnViewportChanged(object? sender, ViewportChangedEventArgs e)
    {
        if (Children.Count == 0 || e.ProjectionChanged || Math.Abs(e.LongitudeOffset) > 180d)
            await Update(); // update immediately when projection has changed or center has moved across 180° longitude
        else
        {
            SetRenderTransform();

            _updateTimer.Run(!UpdateWhileViewportChanging);
        }
    }
}