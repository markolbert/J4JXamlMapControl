﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace J4JSoftware.XamlMapControl
{
    /// <summary>
    /// Container class for an item in a MapItemsControl.
    /// </summary>
    public partial class MapItem : ListBoxItem
    {
        public static readonly DependencyProperty LocationMemberPathProperty = DependencyProperty.Register(
            nameof(LocationMemberPath), typeof(string), typeof(MapItem),
            new PropertyMetadata(null, (o, e) => BindingOperations.SetBinding(
                o, LocationProperty, new Binding { Path = new PropertyPath((string)e.NewValue) })));

        /// <summary>
        /// Gets/sets MapPanel.AutoCollapse.
        /// </summary>
        public bool AutoCollapse
        {
            get { return (bool)GetValue(AutoCollapseProperty); }
            set { SetValue(AutoCollapseProperty, value); }
        }

        /// <summary>
        /// Gets/sets MapPanel.Location.
        /// </summary>
        public Location Location
        {
            get { return (Location)GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }

        /// <summary>
        /// Path to a source property for binding the Location property.
        /// </summary>
        public string LocationMemberPath
        {
            get { return (string)GetValue(LocationMemberPathProperty); }
            set { SetValue(LocationMemberPathProperty, value); }
        }
    }

    /// <summary>
    /// Manages a collection of selectable items on a Map.
    /// </summary>
    public partial class MapItemsControl : ListBox
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MapItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is MapItem;
        }

        public void SelectItems(Predicate<object> predicate)
        {
            if (SelectionMode == SelectionMode.Single)
            {
                throw new InvalidOperationException("SelectionMode must not be Single");
            }

            foreach (var item in Items)
            {
                var selected = predicate(item);

                if (selected != SelectedItems.Contains(item))
                {
                    if (selected)
                    {
                        SelectedItems.Add(item);
                    }
                    else
                    {
                        SelectedItems.Remove(item);
                    }
                }
            }
        }

        public void SelectItemsByLocation(Predicate<Location> predicate)
        {
            SelectItems(item =>
            {
                var loc = J4JSoftware.XamlMapControl.MapPanel.GetLocation(ContainerFromItem(item));
                return loc != null && predicate(loc);
            });
        }

        public void SelectItemsByPosition(Predicate<Point> predicate)
        {
            SelectItems(item =>
            {
                var pos = J4JSoftware.XamlMapControl.MapPanel.GetViewPosition(ContainerFromItem(item));
                return pos.HasValue && predicate(pos.Value);
            });
        }

        public void SelectItemsInRect(Rect rect)
        {
            SelectItemsByPosition(p => rect.Contains(p));
        }

        protected internal void OnItemClicked(FrameworkElement mapItem, bool controlKey, bool shiftKey)
        {
            var item = ItemFromContainer(mapItem);

            if (SelectionMode == SelectionMode.Single)
            {
                // Single -> set only SelectedItem

                if (SelectedItem != item)
                {
                    SelectedItem = item;
                }
                else if (controlKey)
                {
                    SelectedItem = null;
                }
            }
            else if (SelectionMode == SelectionMode.Multiple || controlKey)
            {
                // Multiple or Extended with Ctrl -> toggle item in SelectedItems

                if (SelectedItems.Contains(item))
                {
                    SelectedItems.Remove(item);
                }
                else
                {
                    SelectedItems.Add(item);
                }
            }
            else if (shiftKey && SelectedItem != null)
            {
                // Extended with Shift -> select items in view rectangle

                var p1 = J4JSoftware.XamlMapControl.MapPanel.GetViewPosition(ContainerFromItem(SelectedItem));
                var p2 = J4JSoftware.XamlMapControl.MapPanel.GetViewPosition(mapItem);

                if (p1.HasValue && p2.HasValue)
                {
                    SelectItemsInRect(new Rect(p1.Value, p2.Value));
                }
            }
            else if (SelectedItem != item)
            {
                // Extended without Control or Shift -> set selected item

                SelectedItem = item;
            }
        }
    }
}
