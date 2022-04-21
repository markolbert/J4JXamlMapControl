// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Point = J4JSoftware.XamlMapControl.Point;

namespace J4JSoftware.XamlMapControl
{
    /// <summary>
    /// Optional interface to hold the value of the attached property MapPanel.ParentMap.
    /// </summary>
    public interface IMapElement
    {
        MapBase ParentMap { get; set; }
    }

    /// <summary>
    /// Arranges child elements on a Map at positions specified by the attached property Location,
    /// or in rectangles specified by the attached property BoundingBox.
    /// </summary>
    public partial class MapPanel : Panel, IMapElement
    {
        public static readonly DependencyProperty AutoCollapseProperty = DependencyProperty.RegisterAttached(
            "AutoCollapse", typeof(bool), typeof(MapPanel), new PropertyMetadata(false));

        private MapBase _parentMap;

        public MapBase ParentMap
        {
            get => _parentMap;
            set => SetParentMap(value);
        }

        /// <summary>
        /// Gets a value that controls whether an element's Visibility is automatically
        /// set to Collapsed when it is located outside the visible viewport area.
        /// </summary>
        public static bool GetAutoCollapse(FrameworkElement element)
        {
            return (bool)element.GetValue(AutoCollapseProperty);
        }

        /// <summary>
        /// Sets the AutoCollapse property.
        /// </summary>
        public static void SetAutoCollapse(FrameworkElement element, bool value)
        {
            element.SetValue(AutoCollapseProperty, value);
        }

        /// <summary>
        /// Gets the geodetic Location of an element.
        /// </summary>
        public static Location GetLocation(FrameworkElement element)
        {
            return (Location)element.GetValue(MapPanel.LocationProperty);
        }

        /// <summary>
        /// Sets the geodetic Location of an element.
        /// </summary>
        public static void SetLocation(FrameworkElement element, Location value)
        {
            element.SetValue(MapPanel.LocationProperty, value);
        }

        /// <summary>
        /// Gets the BoundingBox of an element.
        /// </summary>
        public static BoundingBox GetBoundingBox(FrameworkElement element)
        {
            return (BoundingBox)element.GetValue(MapPanel.BoundingBoxProperty);
        }

        /// <summary>
        /// Sets the BoundingBox of an element.
        /// </summary>
        public static void SetBoundingBox(FrameworkElement element, BoundingBox value)
        {
            element.SetValue(MapPanel.BoundingBoxProperty, value);
        }

        /// <summary>
        /// Gets the position of an element in view coordinates.
        /// </summary>
        public static Point? GetViewPosition(FrameworkElement element)
        {
            return (Point?)element.GetValue(MapPanel.ViewPositionProperty);
        }

        /// <summary>
        /// Returns the view position of a Location.
        /// </summary>
        public Point? GetViewPosition(Location location)
        {
            if (location == null)
            {
                return null;
            }

            var position = _parentMap.LocationToView(location);

            if (_parentMap.MapProjection.Type <= MapProjectionType.NormalCylindrical && IsOutsideViewport(position))
            {
                location = new Location(location.Latitude, _parentMap.ConstrainedLongitude(location.Longitude));

                position = _parentMap.LocationToView(location);
            }

            return position;
        }

        /// <summary>
        /// Returns the potentially rotated view rectangle of a BoundingBox.
        /// </summary>
        public ViewRect GetViewRect(BoundingBox boundingBox)
        {
            return GetViewRect(_parentMap.MapProjection.BoundingBoxToRect(boundingBox));
        }

        /// <summary>
        /// Returns the potentially rotated view rectangle of a map coordinate rectangle.
        /// </summary>
        public ViewRect GetViewRect(Rect rect)
        {
            var center = new Point(rect.X + rect.Width / 2d, rect.Y + rect.Height / 2d);
            var position = _parentMap.ViewTransform.MapToView(center);

            if (_parentMap.MapProjection.Type <= MapProjectionType.NormalCylindrical && IsOutsideViewport(position))
            {
                var location = _parentMap.MapProjection.MapToLocation(center);
                if (location != null)
                {
                    location.Longitude = _parentMap.ConstrainedLongitude(location.Longitude);
                    position = _parentMap.LocationToView(location);
                }
            }

            var width = rect.Width * _parentMap.ViewTransform.Scale;
            var height = rect.Height * _parentMap.ViewTransform.Scale;
            var x = position.X - width / 2d;
            var y = position.Y - height / 2d;

            return new ViewRect(x, y, width, height, _parentMap.ViewTransform.Rotation);
        }

        protected virtual void SetParentMap(MapBase map)
        {
            if (_parentMap != null && _parentMap != this)
            {
                _parentMap.ViewportChanged -= OnViewportChanged;
            }

            _parentMap = map;

            if (_parentMap != null && _parentMap != this)
            {
                _parentMap.ViewportChanged += OnViewportChanged;

                OnViewportChanged(new ViewportChangedEventArgs());
            }
        }

        private void OnViewportChanged(object sender, ViewportChangedEventArgs e)
        {
            OnViewportChanged(e);
        }

        protected virtual void OnViewportChanged(ViewportChangedEventArgs e)
        {
            InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            foreach (var element in Children.OfType<FrameworkElement>())
            {
                element.Measure(availableSize);
            }

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_parentMap != null)
            {
                foreach (var element in Children.OfType<FrameworkElement>())
                {
                    var position = GetViewPosition(GetLocation(element));

                    MapPanel.SetViewPosition(element, position);

                    if (GetAutoCollapse(element))
                    {
                        if (position.HasValue && IsOutsideViewport(position.Value))
                        {
                            element.SetValue(VisibilityProperty, Visibility.Collapsed);
                        }
                        else
                        {
                            element.ClearValue(VisibilityProperty);
                        }
                    }

                    try
                    {
                        if (position.HasValue)
                        {
                            ArrangeElement(element, position.Value);
                        }
                        else
                        {
                            var boundingBox = GetBoundingBox(element);

                            if (boundingBox != null)
                            {
                                ArrangeElement(element, GetViewRect(boundingBox));
                            }
                            else
                            {
                                ArrangeElement(element, finalSize);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"MapPanel.ArrangeElement: {ex.Message}");
                    }
                }
            }

            return finalSize;
        }

        private bool IsOutsideViewport(Point point)
        {
            return point.X < 0d || point.X > _parentMap.RenderSize.Width
                || point.Y < 0d || point.Y > _parentMap.RenderSize.Height;
        }

        private static void ArrangeElement(FrameworkElement element, ViewRect rect)
        {
            element.Width = rect.Width;
            element.Height = rect.Height;

            ArrangeElement(element, new Rect(rect.X, rect.Y, rect.Width, rect.Height));

            if (element.RenderTransform is RotateTransform rotateTransform)
            {
                rotateTransform.Angle = rect.Rotation;
            }
            else if (rect.Rotation != 0d)
            {
                rotateTransform = new RotateTransform { Angle = rect.Rotation };
                element.RenderTransform = rotateTransform;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }
        }

        private static void ArrangeElement(FrameworkElement element, Point position)
        {
            var rect = new Rect(position, element.DesiredSize);

            switch (element.HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    rect.X -= rect.Width / 2d;
                    break;

                case HorizontalAlignment.Right:
                    rect.X -= rect.Width;
                    break;

                default:
                    break;
            }

            switch (element.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    rect.Y -= rect.Height / 2d;
                    break;

                case VerticalAlignment.Bottom:
                    rect.Y -= rect.Height;
                    break;

                default:
                    break;
            }

            ArrangeElement(element, rect);
        }

        private static void ArrangeElement(FrameworkElement element, Size parentSize)
        {
            var rect = new Rect(new Point(), element.DesiredSize);

            switch (element.HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    rect.X = (parentSize.Width - rect.Width) / 2d;
                    break;

                case HorizontalAlignment.Right:
                    rect.X = parentSize.Width - rect.Width;
                    break;

                case HorizontalAlignment.Stretch:
                    rect.Width = parentSize.Width;
                    break;

                default:
                    break;
            }

            switch (element.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    rect.Y = (parentSize.Height - rect.Height) / 2d;
                    break;

                case VerticalAlignment.Bottom:
                    rect.Y = parentSize.Height - rect.Height;
                    break;

                case VerticalAlignment.Stretch:
                    rect.Height = parentSize.Height;
                    break;

                default:
                    break;
            }

            ArrangeElement(element, rect);
        }

        private static void ArrangeElement(FrameworkElement element, Rect rect)
        {
            if (element.UseLayoutRounding)
            {
                rect.X = Math.Round(rect.X);
                rect.Y = Math.Round(rect.Y);
                rect.Width = Math.Round(rect.Width);
                rect.Height = Math.Round(rect.Height);
            }

            element.Arrange(rect);
        }

        private static void ParentMapPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is IMapElement mapElement)
            {
                mapElement.ParentMap = e.NewValue as MapBase;
            }
        }
    }
}
