﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl
{
    public partial class MapPanel
    {
        public static readonly DependencyProperty LocationProperty = DependencyProperty.RegisterAttached(
            "Location", typeof(Location), typeof(J4JSoftware.XamlMapControl.MapPanel),
            new PropertyMetadata(null, (o, e) => (((FrameworkElement)o).Parent as J4JSoftware.XamlMapControl.MapPanel)?.InvalidateArrange()));

        public static readonly DependencyProperty BoundingBoxProperty = DependencyProperty.RegisterAttached(
            "BoundingBox", typeof(BoundingBox), typeof(J4JSoftware.XamlMapControl.MapPanel),
            new PropertyMetadata(null, (o, e) => (((FrameworkElement)o).Parent as J4JSoftware.XamlMapControl.MapPanel)?.InvalidateArrange()));

        public static readonly DependencyProperty ParentMapProperty = DependencyProperty.RegisterAttached(
            "ParentMap", typeof(MapBase), typeof(J4JSoftware.XamlMapControl.MapPanel), new PropertyMetadata(null, ParentMapPropertyChanged));

        private static readonly DependencyProperty ViewPositionProperty = DependencyProperty.RegisterAttached(
            "ViewPosition", typeof(Point?), typeof(J4JSoftware.XamlMapControl.MapPanel), new PropertyMetadata(null));

        public MapPanel()
        {
            InitMapElement(this);
        }

        public static void InitMapElement(FrameworkElement element)
        {
            if (element is MapBase)
            {
                element.SetValue(ParentMapProperty, element);
            }
            else
            {
                // Workaround for missing property value inheritance.
                // Loaded and Unloaded handlers set and clear the ParentMap property value.

                element.Loaded += (s, e) => GetParentMap(element);
                element.Unloaded += (s, e) => element.ClearValue(ParentMapProperty);
            }
        }

        public static MapBase GetParentMap(FrameworkElement element)
        {
            var parentMap = (MapBase)element.GetValue(ParentMapProperty);

            if (parentMap == null && (parentMap = FindParentMap(element)) != null)
            {
                element.SetValue(ParentMapProperty, parentMap);
            }

            return parentMap;
        }

        private static MapBase FindParentMap(FrameworkElement element)
        {
            return VisualTreeHelper.GetParent(element) is FrameworkElement parent
                ? ((parent as MapBase) ?? (MapBase)element.GetValue(ParentMapProperty) ?? FindParentMap(parent))
                : null;
        }

        private static void SetViewPosition(FrameworkElement element, Point? viewPosition)
        {
            element.SetValue(ViewPositionProperty, viewPosition);
        }
    }
}
