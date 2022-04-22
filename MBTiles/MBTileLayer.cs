// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.Threading.Tasks;
using J4JSoftware.XamlMapControl;
using J4JSoftware.XamlMapControl.MBTiles;
using Microsoft.UI.Xaml;

namespace MapControl.MBTiles
{
    /// <summary>
    /// MapTileLayer that uses an MBTiles SQLite Database. See https://wiki.openstreetmap.org/wiki/MBTiles.
    /// </summary>
    public class MBTileLayer : MapTileLayer
    {
        public static readonly DependencyProperty FileProperty = DependencyProperty.Register(
            nameof(File), typeof(string), typeof(MBTileLayer),
            new PropertyMetadata(null, async (o, e) => await ((MBTileLayer)o).FilePropertyChanged((string)e.NewValue)));

        public MBTileLayer()
            : this(new TileImageLoader())
        {
        }

        public MBTileLayer(ITileImageLoader tileImageLoader)
            : base(tileImageLoader)
        {
        }

        public string File
        {
            get { return (string)GetValue(FileProperty); }
            set { SetValue(FileProperty, value); }
        }

        /// <summary>
        /// May be overridden to create a derived MBTileSource that handles other tile formats than png and jpg.
        /// </summary>
        protected virtual MBTileSource CreateTileSource(MbTileData tileData)
        {
            return new MBTileSource(tileData);
        }

        private async Task FilePropertyChanged(string file)
        {
            (TileSource as MBTileSource)?.Dispose();

            ClearValue(TileSourceProperty);
            ClearValue(SourceNameProperty);
            ClearValue(DescriptionProperty);
            ClearValue(MinZoomLevelProperty);
            ClearValue(MaxZoomLevelProperty);

            if (file != null)
            {
                var tileData = await MbTileData.CreateAsync(file);

                if (tileData.Metadata.TryGetValue("name", out string sourceName))
                {
                    SourceName = sourceName;
                }

                if (tileData.Metadata.TryGetValue("description", out string description))
                {
                    Description = description;
                }

                if (tileData.Metadata.TryGetValue("minzoom", out sourceName) && int.TryParse(sourceName, out int minZoom))
                {
                    MinZoomLevel = minZoom;
                }

                if (tileData.Metadata.TryGetValue("maxzoom", out sourceName) && int.TryParse(sourceName, out int maxZoom))
                {
                    MaxZoomLevel = maxZoom;
                }

                TileSource = CreateTileSource(tileData);
            }
        }
    }
}
