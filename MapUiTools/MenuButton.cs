// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace J4JSoftware.XamlMapControl.MapUiTools;

public class MenuButton : Button
{
    protected MenuButton( string icon )
    {
        Content = new FontIcon { Glyph = icon };
    }

    protected MenuFlyout CreateMenu()
    {
        var menu = new MenuFlyout();
        Flyout = menu;

        return menu;
    }

    protected IEnumerable<ToggleMenuFlyoutItem> GetMenuItems() =>
        ( (MenuFlyout) Flyout ).Items.OfType<ToggleMenuFlyoutItem>();

    protected ToggleMenuFlyoutItem CreateMenuItem(string? text, object? item, RoutedEventHandler click)
    {
        var menuItem = new ToggleMenuFlyoutItem { Text = text, Tag = item };
        menuItem.Click += click;
        return menuItem;
    }

    protected MenuFlyoutSeparator CreateSeparator() => new MenuFlyoutSeparator();
}