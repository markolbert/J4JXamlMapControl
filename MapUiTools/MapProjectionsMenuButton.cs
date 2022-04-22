// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using J4JSoftware.XamlMapControl.Projections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace J4JSoftware.XamlMapControl.MapUiTools;

[ContentProperty(Name = nameof(MapProjections))]
public class MapProjectionsMenuButton : MenuButton
{
    #region Map property

    public static readonly DependencyProperty MapProperty = DependencyProperty.Register(
        nameof(Map), typeof(MapBase), typeof(MapProjectionsMenuButton),
        new PropertyMetadata(null, (o, e) => ((MapProjectionsMenuButton)o).InitializeMenu()));

    public MapBase? Map
    {
        get => (MapBase)GetValue(MapProperty);
        set => SetValue(MapProperty, value);
    }

    #endregion

    private string? _selectedProjection;

    public MapProjectionsMenuButton()
        : base("\uE809")
    {
        ((INotifyCollectionChanged)MapProjections).CollectionChanged += (s, e) => InitializeMenu();
    }

    public Collection<MapProjectionItem> MapProjections { get; } = new ObservableCollection<MapProjectionItem>();

    private void InitializeMenu()
    {
        if( Map == null )
            return;

        var menu = CreateMenu();

        foreach (var item in MapProjections)
        {
            menu.Items.Add(CreateMenuItem(item.Text, item.Projection, MapProjectionClicked));
        }

        var initialProjection = MapProjections.Select(p => p.Projection).FirstOrDefault();

        if (initialProjection != null)
            SetMapProjection(initialProjection);
    }

    private void MapProjectionClicked(object sender, RoutedEventArgs e)
    {
        var item = (FrameworkElement)sender;
        var projection = (string)item.Tag;

        SetMapProjection(projection);
    }

    private void SetMapProjection(string projection)
    {
        if (_selectedProjection != projection)
        {
            _selectedProjection = projection;
            Map!.MapProjection = MapProjection.Factory.GetProjection(_selectedProjection);
        }

        UpdateCheckedStates();
    }

    private void UpdateCheckedStates()
    {
        foreach (var item in GetMenuItems())
        {
            item.IsChecked = _selectedProjection == (string)item.Tag;
        }
    }
}