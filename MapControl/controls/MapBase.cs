// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using Windows.Foundation;
using J4JSoftware.XamlMapControl.Projections;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// The map control. Displays map content provided by one or more tile or image layers,
/// i.e. MapTileLayerBase or MapImageLayer instances.
/// The visible map area is defined by the Center and ZoomLevel properties.
/// The map can be rotated by an angle that is given by the Heading property.
/// MapBase can contain map overlay child elements like other MapPanels or MapItemsControls.
/// </summary>
public class MapBase : MapPanel
{
    public static TimeSpan ImageFadeDuration { get; set; } = TimeSpan.FromSeconds(0.1);

    #region MapLayer property

    /// <summary>
    /// Gets or sets the base map layer, which is added as first element to the Children collection.
    /// If the layer implements IMapLayer (like MapTileLayer or MapImageLayer), its (non-null) MapBackground
    /// and MapForeground property values are used for the MapBase Background and Foreground properties.
    /// </summary>
    public static readonly DependencyProperty MapLayerProperty = DependencyProperty.Register( nameof( MapLayer ),
        typeof( UIElement ),
        typeof( MapBase ),
        new PropertyMetadata( null,
                              ( o, e ) =>
                                  ( (MapBase) o ).MapLayerPropertyChanged( e.OldValue as UIElement, 
                                                                           e.NewValue as UIElement ) ) );

    public UIElement MapLayer
    {
        get => (UIElement)GetValue(MapLayerProperty);
        set => SetValue(MapLayerProperty, value);
    }

    private void MapLayerPropertyChanged(UIElement? oldLayer, UIElement? newLayer)
    {
        if (oldLayer != null)
        {
            Children.Remove(oldLayer);

            if (oldLayer is IMapLayer mapLayer)
            {
                if (mapLayer.MapBackground != null)
                    ClearValue(BackgroundProperty);

                if (mapLayer.MapForeground != null)
                    ClearValue(ForegroundProperty);
            }
        }

        if (newLayer == null)
            return;

        Children.Insert(0, newLayer);

        if (newLayer is not IMapLayer mapLayer2)
            return;

        if (mapLayer2.MapBackground != null)
            Background = mapLayer2.MapBackground;

        if (mapLayer2.MapForeground != null)
            Foreground = mapLayer2.MapForeground;
    }

    #endregion

    #region MapProjection property

    /// <summary>
    /// Gets or sets the MapProjection used by the map control.
    /// </summary>
    public static readonly DependencyProperty MapProjectionProperty = DependencyProperty.Register(
        nameof( MapProjection ),
        typeof( MapProjection ),
        typeof( MapBase ),
        new PropertyMetadata( new WebMercatorProjection(),
                              ( o, e ) =>
                                  ( (MapBase) o ).MapProjectionPropertyChanged( e.NewValue as MapProjection ) ) );

    public MapProjection MapProjection
    {
        get => (MapProjection)GetValue(MapProjectionProperty);
        set => SetValue(MapProjectionProperty, value);
    }

    private void MapProjectionPropertyChanged(MapProjection? projection)
    {
        _maxLatitude = 90d;

        if (projection?.Type <= MapProjectionType.NormalCylindrical)
        {
            var maxLocation = projection.MapToLocation(new Point(0d, 180d * MapProjection.Wgs84MeterPerDegree));

            if (maxLocation != null && maxLocation.Latitude < 90d)
            {
                _maxLatitude = maxLocation.Latitude;

                var center = Center;
                AdjustCenterProperty(CenterProperty, ref center);
            }
        }

        ResetTransformCenter();
        UpdateTransform(false, true);
    }

    #endregion

    #region ProjectionCenter property

    /// <summary>
    /// Gets or sets an optional center (reference point) for azimuthal projections.
    /// If ProjectionCenter is null, the Center property value will be used instead.
    /// </summary>
    public static readonly DependencyProperty ProjectionCenterProperty = DependencyProperty.Register(
        nameof( ProjectionCenter ),
        typeof( Location ),
        typeof( MapBase ),
        new PropertyMetadata( null, ( o, _ ) => ( (MapBase) o ).ProjectionCenterPropertyChanged() ) );

    public Location? ProjectionCenter
    {
        get => (Location)GetValue(ProjectionCenterProperty);
        set => SetValue(ProjectionCenterProperty, value);
    }

    private void ProjectionCenterPropertyChanged()
    {
        ResetTransformCenter();
        UpdateTransform();
    }

    #endregion

    #region MinZoomLevel property

    /// <summary>
    /// Gets or sets the minimum value of the ZoomLevel and TargetZommLevel properties.
    /// Must be greater than or equal to zero and less than or equal to MaxZoomLevel.
    /// The default value is 1.
    /// </summary>
    public static readonly DependencyProperty MinZoomLevelProperty = DependencyProperty.Register(
        nameof( MinZoomLevel ),
        typeof( double ),
        typeof( MapBase ),
        new PropertyMetadata( 1d, ( o, e ) => ( (MapBase) o ).MinZoomLevelPropertyChanged( (double) e.NewValue ) ) );

    public double MinZoomLevel
    {
        get => (double)GetValue(MinZoomLevelProperty);
        set => SetValue(MinZoomLevelProperty, value);
    }

    private void MinZoomLevelPropertyChanged(double minZoomLevel)
    {
        if (minZoomLevel < 0d || minZoomLevel > MaxZoomLevel)
        {
            minZoomLevel = Math.Min(Math.Max(minZoomLevel, 0d), MaxZoomLevel);

            SetValueInternal(MinZoomLevelProperty, minZoomLevel);
        }

        if (ZoomLevel < minZoomLevel)
            ZoomLevel = minZoomLevel;
    }

    #endregion

    #region MaxZoomLevel property

    /// <summary>
    /// Gets or sets the maximum value of the ZoomLevel and TargetZommLevel properties.
    /// Must be greater than or equal to MinZoomLevel and less than or equal to 22.
    /// The default value is 20.
    /// </summary>
    public static readonly DependencyProperty MaxZoomLevelProperty = DependencyProperty.Register(
        nameof( MaxZoomLevel ),
        typeof( double ),
        typeof( MapBase ),
        new PropertyMetadata( 20d, ( o, e ) => ( (MapBase) o ).MaxZoomLevelPropertyChanged( (double) e.NewValue ) ) );

    public double MaxZoomLevel
    {
        get => (double)GetValue(MaxZoomLevelProperty);
        set => SetValue(MaxZoomLevelProperty, value);
    }

    private void MaxZoomLevelPropertyChanged(double maxZoomLevel)
    {
        if (maxZoomLevel < MinZoomLevel)
        {
            maxZoomLevel = MinZoomLevel;

            SetValueInternal(MaxZoomLevelProperty, maxZoomLevel);
        }

        if (ZoomLevel > maxZoomLevel)
            ZoomLevel = maxZoomLevel;
    }

    #endregion

    #region AnimationDuration property

    /// <summary>
    /// Gets or sets the Duration of the Center, ZoomLevel and Heading animations.
    /// The default value is 0.3 seconds.
    /// </summary>
    public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register(
        nameof(AnimationDuration), typeof(TimeSpan), typeof(MapBase),
        new PropertyMetadata(TimeSpan.FromSeconds(0.3)));

    public TimeSpan AnimationDuration
    {
        get => (TimeSpan)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    #endregion

    #region AnimationEasingFunction property

    /// <summary>
    /// Gets or sets the EasingFunction of the Center, ZoomLevel and Heading animations.
    /// The default value is a QuadraticEase with EasingMode.EaseOut.
    /// </summary>
    public static readonly DependencyProperty AnimationEasingFunctionProperty = DependencyProperty.Register(
        nameof(AnimationEasingFunction), typeof(EasingFunctionBase), typeof(MapBase),
        new PropertyMetadata(new QuadraticEase { EasingMode = EasingMode.EaseOut }));

    public EasingFunctionBase AnimationEasingFunction
    {
        get => (EasingFunctionBase)GetValue(AnimationEasingFunctionProperty);
        set => SetValue(AnimationEasingFunctionProperty, value);
    }

    #endregion

    #region Foreground property

    /// <summary>
    /// Gets or sets the map foreground Brush.
    /// </summary>
    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
        nameof( Foreground ),
        typeof( Brush ),
        typeof( MapBase ),
        new PropertyMetadata( new SolidColorBrush( Colors.Black ) ) );

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    #endregion

    #region Center property

    /// <summary>
    /// Gets or sets the location of the center point of the map.
    /// </summary>
    public static readonly DependencyProperty CenterProperty = DependencyProperty.Register( nameof( Center ),
        typeof( Location ),
        typeof( MapBase ),
        new PropertyMetadata( new Location(),
                              ( o, e ) => ( (MapBase) o ).CenterPropertyChanged( e.NewValue as Location ) ) );

    public Location? Center
    {
        get => (Location)GetValue(CenterProperty);
        set => SetValue(CenterProperty, value);
    }

    private void CenterPropertyChanged(Location? center)
    {
        if (_internalPropertyChange)
            return;

        AdjustCenterProperty(CenterProperty, ref center);
        UpdateTransform();

        if (_centerAnimation == null)
        {
            SetValueInternal(TargetCenterProperty, center);
        }
    }

    #endregion

    #region TargetCenter property

    /// <summary>
    /// Gets or sets the target value of a Center animation.
    /// </summary>
    public static readonly DependencyProperty TargetCenterProperty = DependencyProperty.Register(
        nameof( TargetCenter ),
        typeof( Location ),
        typeof( MapBase ),
        new PropertyMetadata( new Location(),
                              ( o, e ) => ( (MapBase) o ).TargetCenterPropertyChanged( e.NewValue as Location ) ) );

    public Location TargetCenter
    {
        get => (Location)GetValue(TargetCenterProperty);
        set => SetValue(TargetCenterProperty, value);
    }

    private void TargetCenterPropertyChanged(Location? targetCenter)
    {
        if (_internalPropertyChange)
            return;

        AdjustCenterProperty(TargetCenterProperty, ref targetCenter);

        if (targetCenter == null || Center == null || targetCenter.Equals(Center))
            return;

        if (_centerAnimation != null)
            _centerAnimation.Completed -= CenterAnimationCompleted;

        _centerAnimation = new PointAnimation
        {
            From = new Point(Center.Longitude, Center.Latitude),
            To = new Point(ConstrainedLongitude(targetCenter.Longitude), targetCenter.Latitude),
            Duration = AnimationDuration,
            EasingFunction = AnimationEasingFunction
        };

        _centerAnimation.Completed += CenterAnimationCompleted;

        this.BeginAnimation(CenterPointProperty, _centerAnimation);
    }

    #endregion

    #region ZoomLevel property

    /// <summary>
    /// Gets or sets the map zoom level.
    /// </summary>
    public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty.Register(
        nameof(ZoomLevel), typeof(double), typeof(MapBase),
        new PropertyMetadata(1d, (o, e) => ((MapBase)o).ZoomLevelPropertyChanged((double)e.NewValue)));

    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    private void ZoomLevelPropertyChanged(double zoomLevel)
    {
        if (_internalPropertyChange)
            return;

        AdjustZoomLevelProperty(ZoomLevelProperty, ref zoomLevel);
        UpdateTransform();

        if (_zoomLevelAnimation == null)
            SetValueInternal(TargetZoomLevelProperty, zoomLevel);
    }

    #endregion

    #region TargetZoomLevel property

    /// <summary>
    /// Gets or sets the target value of a ZoomLevel animation.
    /// </summary>
    public static readonly DependencyProperty TargetZoomLevelProperty = DependencyProperty.Register(
        nameof(TargetZoomLevel), typeof(double), typeof(MapBase),
        new PropertyMetadata(1d, (o, e) => ((MapBase)o).TargetZoomLevelPropertyChanged((double)e.NewValue)));

    public double TargetZoomLevel
    {
        get => (double)GetValue(TargetZoomLevelProperty);
        set => SetValue(TargetZoomLevelProperty, value);
    }

    private void TargetZoomLevelPropertyChanged(double targetZoomLevel)
    {
        if (_internalPropertyChange)
            return;

        AdjustZoomLevelProperty(TargetZoomLevelProperty, ref targetZoomLevel);

        if (!(Math.Abs(targetZoomLevel - ZoomLevel) > XamlMapControlConstants.ZoomTolerance))
            return;

        if (_zoomLevelAnimation != null)
            _zoomLevelAnimation.Completed -= ZoomLevelAnimationCompleted;

        _zoomLevelAnimation = new DoubleAnimation
        {
            To = targetZoomLevel,
            Duration = AnimationDuration,
            EasingFunction = AnimationEasingFunction
        };

        _zoomLevelAnimation.Completed += ZoomLevelAnimationCompleted;

        this.BeginAnimation(ZoomLevelProperty, _zoomLevelAnimation);
    }

    #endregion

    #region Heading property

    /// <summary>
    /// Gets or sets the map heading, i.e. a clockwise rotation angle in degrees.
    /// </summary>
    public static readonly DependencyProperty HeadingProperty = DependencyProperty.Register( nameof( Heading ),
        typeof( double ),
        typeof( MapBase ),
        new PropertyMetadata( 0d, ( o, e ) => ( (MapBase) o ).HeadingPropertyChanged( (double) e.NewValue ) ) );

    public double Heading
    {
        get => (double)GetValue(HeadingProperty);
        set => SetValue(HeadingProperty, value);
    }

    private void HeadingPropertyChanged(double heading)
    {
        if (_internalPropertyChange)
            return;

        AdjustHeadingProperty(HeadingProperty, ref heading);
        UpdateTransform();

        if (_headingAnimation == null)
            SetValueInternal(TargetHeadingProperty, heading);
    }

    #endregion

    #region TargetHeading property

    /// <summary>
    /// Gets or sets the target value of a Heading animation.
    /// </summary>
    public static readonly DependencyProperty TargetHeadingProperty = DependencyProperty.Register(
        nameof(TargetHeading), typeof(double), typeof(MapBase),
        new PropertyMetadata(0d, (o, e) => ((MapBase)o).TargetHeadingPropertyChanged((double)e.NewValue)));

    public double TargetHeading
    {
        get => (double)GetValue(TargetHeadingProperty);
        set => SetValue(TargetHeadingProperty, value);
    }

    private void TargetHeadingPropertyChanged(double targetHeading)
    {
        if (_internalPropertyChange)
            return;

        AdjustHeadingProperty(TargetHeadingProperty, ref targetHeading);

        if (!(Math.Abs(targetHeading - Heading) > XamlMapControlConstants.HeadingTolerance))
            return;

        var delta = targetHeading - Heading;

        switch (delta)
        {
            case > 180d:
                delta -= 360d;
                break;

            case < -180d:
                delta += 360d;
                break;
        }

        if (_headingAnimation != null)
            _headingAnimation.Completed -= HeadingAnimationCompleted;

        _headingAnimation = new DoubleAnimation
        {
            By = delta,
            Duration = AnimationDuration,
            EasingFunction = AnimationEasingFunction
        };

        _headingAnimation.Completed += HeadingAnimationCompleted;

        this.BeginAnimation(HeadingProperty, _headingAnimation);
    }

    #endregion

    #region ViewScale property

    /// <summary>
    /// Gets the scaling factor from cartesian map coordinates to view coordinates,
    /// i.e. pixels per meter, as a read-only dependency property.
    /// </summary>
    public static readonly DependencyProperty ViewScaleProperty = DependencyProperty.Register(
        nameof(ViewScale), typeof(double), typeof(MapBase), new PropertyMetadata(0d));

    public double ViewScale => (double)GetValue(ViewScaleProperty);

    #endregion

    #region CenterPoint property (internal)

    internal static readonly DependencyProperty CenterPointProperty = DependencyProperty.Register(
        "CenterPoint", typeof(Windows.Foundation.Point), typeof(MapBase),
        new PropertyMetadata(new Windows.Foundation.Point(), (o, e) =>
        {
            var center = (Windows.Foundation.Point)e.NewValue;
            ((MapBase)o).CenterPointPropertyChanged(new Location(center.Y, center.X));
        }));

    #endregion

    /// <summary>
    /// Raised when the current map viewport has changed.
    /// </summary>
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;

    private PointAnimation? _centerAnimation;
    private DoubleAnimation? _zoomLevelAnimation;
    private DoubleAnimation? _headingAnimation;
    private Location? _transformCenter;
    private Point _viewCenter;
    private double _centerLongitude;
    private double _maxLatitude = 90d;
    private bool _internalPropertyChange;

    public MapBase()
    {
        // set Background by Style to enable resetting by ClearValue in MapLayerPropertyChanged
        var style = new Style(typeof(MapBase));
        style.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Colors.White)));
        Style = style;

        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Clip = new RectangleGeometry
        {
            Rect = new Rect(0d, 0d, e.NewSize.Width, e.NewSize.Height)
        };

        ResetTransformCenter();
        UpdateTransform();
    }

    private void SetViewScale(double scale)
    {
        SetValue(ViewScaleProperty, scale);
    }

    /// <summary>
    /// Gets the ViewTransform instance that is used to transform between cartesian map coordinates
    /// and view coordinates.
    /// </summary>
    public ViewTransform ViewTransform { get; } = new ViewTransform();

    /// <summary>
    /// Gets the horizontal and vertical scaling factors from cartesian map coordinates to view
    /// coordinates at the specified location, i.e. pixels per meter.
    /// </summary>
    public Vector GetScale(Location location)
    {
        return ViewTransform.Scale * MapProjection.GetRelativeScale(location);
    }

    /// <summary>
    /// Transforms a Location in geographic coordinates to a Point in view coordinates.
    /// </summary>
    public Point LocationToView(Location? location)
    {
        return ViewTransform.MapToView(MapProjection.LocationToMap(location));
    }

    /// <summary>
    /// Transforms a Point in view coordinates to a Location in geographic coordinates.
    /// </summary>
    public Location? ViewToLocation(Point point)
    {
        return MapProjection.MapToLocation(ViewTransform.ViewToMap(point));
    }

    /// <summary>
    /// Transforms a Rect in view coordinates to a BoundingBox in geographic coordinates.
    /// </summary>
    public BoundingBox? ViewRectToBoundingBox(Rect rect)
    {
        var p1 = ViewTransform.ViewToMap(new Point(rect.X, rect.Y));
        var p2 = ViewTransform.ViewToMap(new Point(rect.X, rect.Y + rect.Height));
        var p3 = ViewTransform.ViewToMap(new Point(rect.X + rect.Width, rect.Y));
        var p4 = ViewTransform.ViewToMap(new Point(rect.X + rect.Width, rect.Y + rect.Height));

        rect.X = Math.Min(p1.X, Math.Min(p2.X, Math.Min(p3.X, p4.X)));
        rect.Y = Math.Min(p1.Y, Math.Min(p2.Y, Math.Min(p3.Y, p4.Y)));
        rect.Width = Math.Max(p1.X, Math.Max(p2.X, Math.Max(p3.X, p4.X))) - rect.X;
        rect.Height = Math.Max(p1.Y, Math.Max(p2.Y, Math.Max(p3.Y, p4.Y))) - rect.Y;

        return MapProjection.RectToBoundingBox(rect);
    }

    /// <summary>
    /// Sets a temporary center point in view coordinates for scaling and rotation transformations.
    /// This center point is automatically reset when the Center property is set by application code.
    /// </summary>
    public void SetTransformCenter(Point center)
    {
        _transformCenter = ViewToLocation(center);
        _viewCenter = _transformCenter != null ? center : new Point(RenderSize.Width / 2d, RenderSize.Height / 2d);
    }

    /// <summary>
    /// Resets the temporary transform center point set by SetTransformCenter.
    /// </summary>
    public void ResetTransformCenter()
    {
        _transformCenter = null;
        _viewCenter = new Point(RenderSize.Width / 2d, RenderSize.Height / 2d);
    }

    /// <summary>
    /// Changes the Center property according to the specified translation in view coordinates.
    /// </summary>
    public void TranslateMap(Vector translation)
    {
        if (_transformCenter != null)
        {
            ResetTransformCenter();
            UpdateTransform();
        }

        if( translation.X == 0d && translation.Y == 0d )
            return;

        var center = ViewToLocation(_viewCenter - translation);
        if (center != null)
            Center = center;
    }

    /// <summary>
    /// Changes the Center, Heading and ZoomLevel properties according to the specified
    /// view coordinate translation, rotation and scale delta values. Rotation and scaling
    /// is performed relative to the specified center point in view coordinates.
    /// </summary>
    public void TransformMap(Point center, Vector translation, double rotation, double scale)
    {
        if (rotation != 0d || Math.Abs( scale - 1d ) > XamlMapControlConstants.ScaleTolerance )
        {
            SetTransformCenter(center);
            _viewCenter += translation;

            if (rotation != 0d)
            {
                var heading = (((Heading + rotation) % 360d) + 360d) % 360d;

                SetValueInternal(HeadingProperty, heading);
                SetValueInternal(TargetHeadingProperty, heading);
            }

            if (Math.Abs( scale - 1d ) > XamlMapControlConstants.ScaleTolerance)
            {
                var zoomLevel = Math.Min(Math.Max(ZoomLevel + Math.Log(scale, 2d), MinZoomLevel), MaxZoomLevel);

                SetValueInternal(ZoomLevelProperty, zoomLevel);
                SetValueInternal(TargetZoomLevelProperty, zoomLevel);
            }

            UpdateTransform(true);
        }
        else
        {
            TranslateMap(translation); // more precise
        }
    }

    /// <summary>
    /// Sets the value of the TargetZoomLevel property while retaining the specified center point
    /// in view coordinates.
    /// </summary>
    public void ZoomMap( Point center, double zoomLevel )
    {
        zoomLevel = Math.Min( Math.Max( zoomLevel, MinZoomLevel ), MaxZoomLevel );

        if( !( Math.Abs( TargetZoomLevel - zoomLevel ) > XamlMapControlConstants.ZoomTolerance ) )
            return;

        SetTransformCenter( center );
        TargetZoomLevel = zoomLevel;
    }

    /// <summary>
    /// Sets the TargetZoomLevel and TargetCenter properties so that the specified bounding box
    /// fits into the current view. The TargetHeading property is set to zero.
    /// </summary>
    public void ZoomToBounds(BoundingBox boundingBox)
    {
        var rect = MapProjection.BoundingBoxToRect(boundingBox);
        var center = new Point(rect.X + rect.Width / 2d, rect.Y + rect.Height / 2d);
        var targetCenter = MapProjection.MapToLocation(center);

        if( targetCenter == null )
            return;

        var scale = Math.Min(RenderSize.Width / rect.Width, RenderSize.Height / rect.Height);

        TargetZoomLevel = ViewTransform.ScaleToZoomLevel(scale);
        TargetCenter = targetCenter;
        TargetHeading = 0d;
    }

    internal double ConstrainedLongitude( double longitude )
    {
        if( Center == null )
            throw new NullReferenceException( $"Undefined {nameof( Center )}" );

        var offset = longitude - Center.Longitude;

        longitude = offset switch
        {
            > 180d => Center.Longitude - 360d + offset % 360d,
            < -180d => Center.Longitude + 360d + offset % 360d,
            _ => longitude
        };

        return longitude;
    }

    private void AdjustCenterProperty( DependencyProperty property, ref Location? center )
    {
        var reset = false;

        if( center == null )
        {
            center = new Location();
            reset = true;
        }
        else
            if(
                center.Latitude < -_maxLatitude
             || center.Latitude > _maxLatitude
             || center.Longitude < -180d
             || center.Longitude > 180d )
            {
                center = new Location( Math.Min( Math.Max( center.Latitude, -_maxLatitude ), _maxLatitude ),
                                       Location.NormalizeLongitude( center.Longitude ) );
                reset = true;
            }

        if( reset )
            SetValueInternal( property, center );
    }

    private void CenterAnimationCompleted(object? sender, object e)
    {
        if( _centerAnimation == null )
            return;

        _centerAnimation.Completed -= CenterAnimationCompleted;
        _centerAnimation = null;

        this.BeginAnimation(CenterPointProperty, null);
    }

    private void CenterPointPropertyChanged(Location center)
    {
        if( _centerAnimation == null )
            return;

        SetValueInternal(CenterProperty, center);
        UpdateTransform();
    }

    private void AdjustZoomLevelProperty(DependencyProperty property, ref double zoomLevel)
    {
        if( !( zoomLevel < MinZoomLevel ) && !( zoomLevel > MaxZoomLevel ) )
            return;

        zoomLevel = Math.Min(Math.Max(zoomLevel, MinZoomLevel), MaxZoomLevel);

        SetValueInternal(property, zoomLevel);
    }

    private void ZoomLevelAnimationCompleted(object? sender, object e)
    {
        if( _zoomLevelAnimation == null )
            return;

        SetValueInternal(ZoomLevelProperty, TargetZoomLevel);
        UpdateTransform(true);

        _zoomLevelAnimation.Completed -= ZoomLevelAnimationCompleted;
        _zoomLevelAnimation = null;

        this.BeginAnimation(ZoomLevelProperty, null);
    }

    private void AdjustHeadingProperty(DependencyProperty property, ref double heading)
    {
        if( !( heading < 0d ) && !( heading > 360d ) )
            return;

        heading = ((heading % 360d) + 360d) % 360d;

        SetValueInternal(property, heading);
    }

    private void HeadingAnimationCompleted(object? sender, object e)
    {
        if( _headingAnimation == null )
            return;

        SetValueInternal(HeadingProperty, TargetHeading);
        UpdateTransform();

        _headingAnimation.Completed -= HeadingAnimationCompleted;
        _headingAnimation = null;

        this.BeginAnimation(HeadingProperty, null);
    }

    private void SetValueInternal(DependencyProperty property, object? value)
    {
        _internalPropertyChange = true;

        SetValue(property, value);

        _internalPropertyChange = false;
    }

    private void UpdateTransform(bool resetTransformCenter = false, bool projectionChanged = false)
    {
        var viewScale = ViewTransform.ZoomLevelToScale(ZoomLevel);
        var projection = MapProjection;

        if( ProjectionCenter == null )
        {
            if( Center == null )
                return;

            projection.Center = Center;
        }
        else projection.Center = ProjectionCenter;

        var mapCenter = projection.LocationToMap(_transformCenter ?? Center);

        if( !MapProjection.IsValid( mapCenter ) )
            return;

        ViewTransform.SetTransform(mapCenter, _viewCenter, viewScale, Heading);

        if (_transformCenter != null)
        {
            var center = ViewToLocation(new Point(RenderSize.Width / 2d, RenderSize.Height / 2d));

            if (center != null)
            {
                center.Longitude = Location.NormalizeLongitude(center.Longitude);

                if (center.Latitude < -_maxLatitude || center.Latitude > _maxLatitude)
                {
                    center.Latitude = Math.Min(Math.Max(center.Latitude, -_maxLatitude), _maxLatitude);
                    resetTransformCenter = true;
                }

                SetValueInternal(CenterProperty, center);

                if (_centerAnimation == null)
                {
                    SetValueInternal(TargetCenterProperty, center);
                }

                if (resetTransformCenter)
                {
                    ResetTransformCenter();

                    if (ProjectionCenter == null)
                    {
                        if (Center == null)
                            return;

                        projection.Center = Center;
                    }
                    else projection.Center = ProjectionCenter;

                    mapCenter = projection.LocationToMap(center);

                    if (MapProjection.IsValid(mapCenter))
                        ViewTransform.SetTransform(mapCenter, _viewCenter, viewScale, Heading);
                }
            }
        }

        SetViewScale(ViewTransform.Scale);

        if( Center == null )
            return;

        OnViewportChanged(new ViewportChangedEventArgs(projectionChanged, Center.Longitude - _centerLongitude));
        
        _centerLongitude = Center.Longitude;
    }

    protected override void OnViewportChanged(ViewportChangedEventArgs e)
    {
        base.OnViewportChanged(e);

        ViewportChanged?.Invoke(this, e);
    }
}