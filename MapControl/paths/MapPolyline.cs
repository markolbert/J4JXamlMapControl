// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// A polyline defined by a collection of Locations.
/// </summary>
public class MapPolyline : MapPath
{
    #region Locations property

    /// <summary>
    /// The Locations that define the polyline points.
    /// </summary>
    public static readonly DependencyProperty LocationsProperty = DependencyProperty.Register(
        nameof(Locations), typeof(IEnumerable<Location>), typeof(MapPolyline),
        new PropertyMetadata(null, (o, e) => ((MapPolyline)o).DataCollectionPropertyChanged(e)));

    [TypeConverter(typeof(LocationCollectionConverter))]
    public IEnumerable<Location>? Locations
    {
        get => (IEnumerable<Location>?)GetValue(LocationsProperty);
        set => SetValue(LocationsProperty, value);
    }

    #endregion

    public MapPolyline()
    {
        Data = new PathGeometry();
    }

    protected override void UpdateData()
    {
        var pathFigures = ((PathGeometry)Data).Figures;
        pathFigures.Clear();

        if( ParentMap == null || Locations == null )
            return;

        var longitudeOffset = GetLongitudeOffset(Location ?? Locations.FirstOrDefault());

        AddPolylineLocations(pathFigures, Locations, longitudeOffset, false);
    }
}