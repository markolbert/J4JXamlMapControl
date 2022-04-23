// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using J4JSoftware.XamlMapControl;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace J4JSoftware.XamlMapControl.MapUiTools;

[ContentProperty(Name = nameof(MapLayers))]
public class MapLayersMenuButton : MenuButton
{
    #region Map property

    public static readonly DependencyProperty MapProperty = DependencyProperty.Register(
        nameof(Map), typeof(MapBase), typeof(MapLayersMenuButton),
        new PropertyMetadata(null, (o, e) => ((MapLayersMenuButton)o).InitializeMenu()));

    public MapBase? Map
    {
        get => (MapBase)GetValue(MapProperty);
        set => SetValue(MapProperty, value);
    }

    #endregion

    private UIElement? _selectedLayer;

    public MapLayersMenuButton()
        : base("\uE81E")
    {
        ((INotifyCollectionChanged)MapLayers).CollectionChanged += (s, e) => InitializeMenu();
        ((INotifyCollectionChanged)MapOverlays).CollectionChanged += (s, e) => InitializeMenu();
    }

    public Collection<MapLayerItem> MapLayers { get; } = new ObservableCollection<MapLayerItem>();

    public Collection<MapLayerItem> MapOverlays { get; } = new ObservableCollection<MapLayerItem>();

    private void InitializeMenu()
    {
        if( Map == null )
            return;

        var menu = CreateMenu();

        foreach (var item in MapLayers)
        {
            menu.Items.Add(CreateMenuItem(item.Text, item.Layer, MapLayerClicked));
        }

        var initialLayer = MapLayers.Select(l => l.Layer).FirstOrDefault();

        if (MapOverlays.Count > 0)
        {
            if (initialLayer != null)
                menu.Items.Add(CreateSeparator());

            foreach (var item in MapOverlays)
            {
                menu.Items.Add(CreateMenuItem(item.Text, item.Layer, MapOverlayClicked));
            }
        }

        if (initialLayer != null)
            SetMapLayer(initialLayer);
    }

    private void MapLayerClicked(object sender, RoutedEventArgs e)
    {
        var item = (FrameworkElement)sender;
        var layer = (UIElement)item.Tag;

        SetMapLayer(layer);
    }

    private void MapOverlayClicked(object sender, RoutedEventArgs e)
    {
        var item = (FrameworkElement)sender;
        var layer = (UIElement)item.Tag;

        ToggleMapOverlay(layer);
    }

    private void SetMapLayer(UIElement layer)
    {
        if (_selectedLayer != layer)
        {
            _selectedLayer = layer;
            Map!.MapLayer = _selectedLayer;
        }

        UpdateCheckedStates();
    }

    private void ToggleMapOverlay(UIElement layer)
    {
        if (Map!.Children.Contains(layer))
            Map.Children.Remove(layer);
        else
        {
            var index = 1;

            foreach (var overlay in MapOverlays.Select(l => l.Layer))
            {
                if (overlay == layer)
                {
                    Map.Children.Insert(index, layer);
                    break;
                }

                if (Map.Children.Contains(overlay))
                    index++;
            }
        }

        UpdateCheckedStates();
    }

    private void UpdateCheckedStates()
    {
        foreach (var item in GetMenuItems())
        {
            item.IsChecked = Map!.Children.Contains((UIElement)item.Tag);
        }
    }
}