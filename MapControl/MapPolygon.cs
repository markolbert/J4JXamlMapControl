﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl
{
    /// <summary>
    /// A polygon defined by a collection of Locations.
    /// </summary>
    public class MapPolygon : MapPath
    {
        public static readonly DependencyProperty LocationsProperty = DependencyProperty.Register(
            nameof(Locations), typeof(IEnumerable<Location>), typeof(MapPolygon),
            new PropertyMetadata(null, (o, e) => ((MapPolygon)o).DataCollectionPropertyChanged(e)));

        /// <summary>
        /// Gets or sets the Locations that define the polygon points.
        /// </summary>
#if !UWP
        [System.ComponentModel.TypeConverter(typeof(LocationCollectionConverter))]
#endif
        public IEnumerable<Location> Locations
        {
            get { return (IEnumerable<Location>)GetValue(LocationsProperty); }
            set { SetValue(LocationsProperty, value); }
        }

        public MapPolygon()
        {
            Data = new PathGeometry();
        }

        protected override void UpdateData()
        {
            var pathFigures = ((PathGeometry)Data).Figures;
            pathFigures.Clear();

            if (ParentMap != null && Locations != null)
            {
                var longitudeOffset = GetLongitudeOffset(Location ?? Locations.FirstOrDefault());

                AddPolylineLocations(pathFigures, Locations, longitudeOffset, true);
            }
        }
    }
}
