﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Manages a collection of selectable items on a Map.
/// </summary>
public class MapItemsControl : ListBox
{
    public MapItemsControl()
    {
        DefaultStyleKey = typeof( MapItemsControl );
        MapPanel.InitMapElement( this );
    }

    public new FrameworkElement ContainerFromItem( object item )
    {
        return (FrameworkElement) base.ContainerFromItem( item );
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new MapItem();
    }

    protected override bool IsItemItsOwnContainerOverride( object item )
    {
        return item is MapItem;
    }

    public void SelectItems( Predicate<object> predicate )
    {
        if( SelectionMode == SelectionMode.Single )
            throw new InvalidOperationException( "SelectionMode must not be Single" );

        foreach( var item in Items )
        {
            var selected = predicate( item );

            if( selected == SelectedItems.Contains( item ) )
                continue;

            if( selected )
                SelectedItems.Add( item );
            else SelectedItems.Remove( item );
        }
    }

    public void SelectItemsByLocation( Predicate<Location> predicate ) =>
        SelectItems( item =>
        {
            var loc = J4JSoftware.XamlMapControl.MapPanel.GetLocation( ContainerFromItem( item ) );
            return loc != null && predicate( loc );
        } );

    public void SelectItemsByPosition( Predicate<Point> predicate ) =>
        SelectItems( item =>
        {
            var pos = J4JSoftware.XamlMapControl.MapPanel.GetViewPosition( ContainerFromItem( item ) );
            return pos.HasValue && predicate( pos.Value );
        } );

    public void SelectItemsInRect( Rect rect ) => SelectItemsByPosition( p => rect.Contains( p ) );

    protected internal void OnItemClicked( FrameworkElement mapItem, bool controlKey, bool shiftKey )
    {
        var item = ItemFromContainer( mapItem );

        if( SelectionMode == SelectionMode.Single )
        {
            // Single -> set only SelectedItem
            if( SelectedItem != item )
                SelectedItem = item;
            else
                if( controlKey )
                    SelectedItem = null;
        }
        else
            if( SelectionMode == SelectionMode.Multiple || controlKey )
            {
                // Multiple or Extended with Ctrl -> toggle item in SelectedItems
                if( SelectedItems.Contains( item ) )
                    SelectedItems.Remove( item );
                else SelectedItems.Add( item );
            }
            else
                if( shiftKey && SelectedItem != null )
                {
                    // Extended with Shift -> select items in view rectangle
                    var p1 = J4JSoftware.XamlMapControl.MapPanel.GetViewPosition(
                        ContainerFromItem( SelectedItem ) );
                    var p2 = J4JSoftware.XamlMapControl.MapPanel.GetViewPosition( mapItem );

                    if( p1.HasValue && p2.HasValue )
                        SelectItemsInRect( new Rect( p1.Value, p2.Value ) );
                }

                // Extended without Control or Shift -> set selected item
                else
                    if( SelectedItem != item )
                        SelectedItem = item;
    }
}