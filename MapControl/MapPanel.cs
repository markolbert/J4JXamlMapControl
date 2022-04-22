// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using J4JSoftware.XamlMapControl.Projections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Arranges child elements on a Map at positions specified by the attached property Location,
/// or in rectangles specified by the attached property BoundingBox.
/// </summary>
public class MapPanel : Panel, IMapElement
{
    #region AutoCollapse property

    /// <summary>
    /// A value that controls whether an element's Visibility is automatically
    /// set to Collapsed when it is located outside the visible viewport area.
    /// </summary>
    public static readonly DependencyProperty AutoCollapseProperty = DependencyProperty.RegisterAttached(
        "AutoCollapse",
        typeof( bool ),
        typeof( MapPanel ),
        new PropertyMetadata( false ) );

    public static bool GetAutoCollapse( FrameworkElement element )
    {
        var retVal = element.GetValue( AutoCollapseProperty );

        if( retVal != null )
            return (bool) retVal;

        return false;
    }

    public static void SetAutoCollapse( FrameworkElement element, bool value ) =>
        element.SetValue( AutoCollapseProperty, value );

    #endregion

    #region Location property

    /// <summary>
    /// The geodetic Location of an element.
    /// </summary>
    public static readonly DependencyProperty LocationProperty = DependencyProperty.RegisterAttached( "Location",
        typeof( Location ),
        typeof( MapPanel ),
        new PropertyMetadata( null,
                              ( o, _ ) => ( ( (FrameworkElement) o ).Parent as MapPanel )
                                ?.InvalidateArrange() ) );

    public static Location? GetLocation( FrameworkElement element ) =>
        element.GetValue( MapPanel.LocationProperty ) as Location;

    public static void SetLocation( FrameworkElement element, Location value ) =>
        element.SetValue( MapPanel.LocationProperty, value );

    #endregion

    #region BoundingBox property

    /// <summary>
    /// The BoundingBox of an element.
    /// </summary>
    public static readonly DependencyProperty BoundingBoxProperty = DependencyProperty.RegisterAttached( "BoundingBox",
        typeof( BoundingBox ),
        typeof( MapPanel ),
        new PropertyMetadata( null,
                              ( o, _ ) => ( ( (FrameworkElement) o ).Parent as MapPanel )
                                ?.InvalidateArrange() ) );

    public static BoundingBox? GetBoundingBox( FrameworkElement element ) =>
        element.GetValue( MapPanel.BoundingBoxProperty ) as BoundingBox;

    public static void SetBoundingBox( FrameworkElement element, BoundingBox value ) =>
        element.SetValue( MapPanel.BoundingBoxProperty, value );

    #endregion

    #region ParentMap property

    public static readonly DependencyProperty ParentMapProperty = DependencyProperty.RegisterAttached(
        nameof( ParentMap ),
        typeof( MapBase ),
        typeof( MapPanel ),
        new PropertyMetadata( null, ParentMapPropertyChanged ) );

    public MapBase? ParentMap
    {
        get => _parentMap;
        set => SetParentMap( value );
    }

    public static MapBase? GetParentMap( FrameworkElement element )
    {
        var parentMap = element.GetValue( ParentMapProperty ) as MapBase;

        if( parentMap == null && ( parentMap = FindParentMap( element ) ) != null )
            element.SetValue( ParentMapProperty, parentMap );

        return parentMap;
    }

    private static MapBase? FindParentMap( FrameworkElement element )
    {
        if( VisualTreeHelper.GetParent( element ) is not FrameworkElement parent )
            return null;

        var parentMap = parent as MapBase;
        if( parentMap != null )
            return parentMap;

        if( element.GetValue( ParentMapProperty ) is MapBase temp )
            return temp;

        return FindParentMap( parent );
    }

    #endregion

    #region ViewPosition property (private)

    /// <summary>
    /// The position of an element in view coordinates.
    /// </summary>
    private static readonly DependencyProperty ViewPositionProperty = DependencyProperty.RegisterAttached(
        "ViewPosition",
        typeof( Point? ),
        typeof( MapPanel ),
        new PropertyMetadata( null ) );

    public static Point? GetViewPosition( FrameworkElement element )
    {
        return (Point?) element.GetValue( MapPanel.ViewPositionProperty );
    }

    #endregion

    private MapBase? _parentMap;

    public MapPanel()
    {
        InitMapElement( this );
    }

    public static void InitMapElement( FrameworkElement element )
    {
        if( element is MapBase )
            element.SetValue( ParentMapProperty, element );
        else
        {
            // Workaround for missing property value inheritance.
            // Loaded and Unloaded handlers set and clear the ParentMap property value.
            element.Loaded += ( _, _ ) => GetParentMap( element );
            element.Unloaded += ( _, _ ) => element.ClearValue( ParentMapProperty );
        }
    }

    /// <summary>
    /// Returns the view position of a Location.
    /// </summary>
    public Point? GetViewPosition( Location? location )
    {
        if( _parentMap == null || location == null )
            return null;

        var position = _parentMap.LocationToView( location );

        if( _parentMap.MapProjection.Type > MapProjectionType.NormalCylindrical
        || !IsOutsideViewport( position ) )
            return position;

        location = new Location( location.Latitude, _parentMap.ConstrainedLongitude( location.Longitude ) );
        position = _parentMap.LocationToView( location );

        return position;
    }

    /// <summary>
    /// Returns the potentially rotated view rectangle of a BoundingBox.
    /// </summary>
    public ViewRect GetViewRect( BoundingBox boundingBox )
    {
        if( _parentMap == null )
            throw new NullReferenceException( $"Undefined {nameof( _parentMap )}" );

        return GetViewRect( _parentMap.MapProjection.BoundingBoxToRect( boundingBox ) );
    }

    /// <summary>
    /// Returns the potentially rotated view rectangle of a map coordinate rectangle.
    /// </summary>
    public ViewRect GetViewRect( Rect rect )
    {
        if( _parentMap == null )
            throw new NullReferenceException( $"Undefined {nameof( _parentMap )}" );

        var center = new Point( rect.X + rect.Width / 2d, rect.Y + rect.Height / 2d );
        var position = _parentMap.ViewTransform.MapToView( center );

        if( _parentMap.MapProjection.Type <= MapProjectionType.NormalCylindrical && IsOutsideViewport( position ) )
        {
            var location = _parentMap.MapProjection.MapToLocation( center );
            if( location != null )
            {
                location.Longitude = _parentMap.ConstrainedLongitude( location.Longitude );
                position = _parentMap.LocationToView( location );
            }
        }

        var width = rect.Width * _parentMap.ViewTransform.Scale;
        var height = rect.Height * _parentMap.ViewTransform.Scale;
        var x = position.X - width / 2d;
        var y = position.Y - height / 2d;

        return new ViewRect( x, y, width, height, _parentMap.ViewTransform.Rotation );
    }

    protected virtual void SetParentMap( MapBase? map )
    {
        if( _parentMap != null && _parentMap != this )
            _parentMap.ViewportChanged -= OnViewportChanged;

        _parentMap = map;

        if( _parentMap == null || _parentMap == this )
            return;

        _parentMap.ViewportChanged += OnViewportChanged;

        OnViewportChanged( new ViewportChangedEventArgs() );
    }

    private void OnViewportChanged( object? sender, ViewportChangedEventArgs e ) => OnViewportChanged( e );
    protected virtual void OnViewportChanged( ViewportChangedEventArgs e ) => InvalidateArrange();

    // ReSharper disable once RedundantAssignment
    protected override Size MeasureOverride( Size availableSize )
    {
        availableSize = new Size( double.PositiveInfinity, double.PositiveInfinity );

        foreach( var element in Children.OfType<FrameworkElement>() )
        {
            element.Measure( availableSize );
        }

        return new Size();
    }

    protected override Size ArrangeOverride( Size finalSize )
    {
        if( _parentMap == null )
            return finalSize;

        foreach( var element in Children.OfType<FrameworkElement>() )
        {
            var position = GetViewPosition( GetLocation( element ) );

            SetViewPosition( element, position );

            if( GetAutoCollapse( element ) )
            {
                if( position.HasValue && IsOutsideViewport( position.Value ) )
                    element.SetValue( VisibilityProperty, Visibility.Collapsed );
                else element.ClearValue( VisibilityProperty );
            }

            try
            {
                if( position.HasValue )
                    ArrangeElement( element, position.Value );
                else
                {
                    if( GetBoundingBox( element ) is {} boundingBox )
                        ArrangeElement( element, GetViewRect( boundingBox ) );
                    else ArrangeElement( element, finalSize );
                }
            }
            catch( Exception ex )
            {
                Debug.WriteLine( $"MapPanel.ArrangeElement: {ex.Message}" );
            }
        }

        return finalSize;
    }

    private void SetViewPosition( FrameworkElement element, Point? viewPosition ) =>
        element.SetValue( ViewPositionProperty, viewPosition );

    private bool IsOutsideViewport( Point point )
    {
        if( _parentMap == null )
            throw new NullReferenceException( $"{nameof( _parentMap )} is undefined" );

        return point.X < 0d
         || point.X > _parentMap.RenderSize.Width
         || point.Y < 0d
         || point.Y > _parentMap.RenderSize.Height;
    }

    private static void ArrangeElement( FrameworkElement element, ViewRect rect )
    {
        element.Width = rect.Width;
        element.Height = rect.Height;

        ArrangeElement( element, new Rect( rect.X, rect.Y, rect.Width, rect.Height ) );

        if( element.RenderTransform is RotateTransform rotateTransform )
            rotateTransform.Angle = rect.Rotation;
        else
            if( rect.Rotation != 0d )
            {
                rotateTransform = new RotateTransform { Angle = rect.Rotation };
                element.RenderTransform = rotateTransform;
                element.RenderTransformOrigin = new Point( 0.5, 0.5 );
            }
    }

    private static void ArrangeElement( FrameworkElement element, Point position )
    {
        var rect = new Rect( position, element.DesiredSize );

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch( element.HorizontalAlignment )
        {
            case HorizontalAlignment.Center:
                rect.X -= rect.Width / 2d;
                break;

            case HorizontalAlignment.Right:
                rect.X -= rect.Width;
                break;
        }

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch( element.VerticalAlignment )
        {
            case VerticalAlignment.Center:
                rect.Y -= rect.Height / 2d;
                break;

            case VerticalAlignment.Bottom:
                rect.Y -= rect.Height;
                break;
        }

        ArrangeElement( element, rect );
    }

    private static void ArrangeElement( FrameworkElement element, Size parentSize )
    {
        var rect = new Rect( new Point(), element.DesiredSize );

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch( element.HorizontalAlignment )
        {
            case HorizontalAlignment.Center:
                rect.X = ( parentSize.Width - rect.Width ) / 2d;
                break;

            case HorizontalAlignment.Right:
                rect.X = parentSize.Width - rect.Width;
                break;

            case HorizontalAlignment.Stretch:
                rect.Width = parentSize.Width;
                break;
        }

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch( element.VerticalAlignment )
        {
            case VerticalAlignment.Center:
                rect.Y = ( parentSize.Height - rect.Height ) / 2d;
                break;

            case VerticalAlignment.Bottom:
                rect.Y = parentSize.Height - rect.Height;
                break;

            case VerticalAlignment.Stretch:
                rect.Height = parentSize.Height;
                break;
        }

        ArrangeElement( element, rect );
    }

    private static void ArrangeElement( FrameworkElement element, Rect rect )
    {
        if( element.UseLayoutRounding )
        {
            rect.X = Math.Round( rect.X );
            rect.Y = Math.Round( rect.Y );
            rect.Width = Math.Round( rect.Width );
            rect.Height = Math.Round( rect.Height );
        }

        element.Arrange( rect );
    }

    private static void ParentMapPropertyChanged( DependencyObject obj, DependencyPropertyChangedEventArgs e )
    {
        if( obj is IMapElement mapElement )
        {
            mapElement.ParentMap = e.NewValue as MapBase;
        }
    }
}