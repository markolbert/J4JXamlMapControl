﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl
{
    /// <summary>
    /// Displays web mercator map tiles.
    /// </summary>
    public class MapTileLayer : MapTileLayerBase
    {
        public const int TileSize = 256;

        public static readonly Point MapTopLeft = new Point(
            -180d * MapProjection.Wgs84MeterPerDegree, 180d * MapProjection.Wgs84MeterPerDegree);

        /// <summary>
        /// A default MapTileLayer using OpenStreetMap data.
        /// </summary>
        public static MapTileLayer OpenStreetMapTileLayer
        {
            get
            {
                return new MapTileLayer
                {
                    TileSource = new TileSource { UriFormat = "https://tile.openstreetmap.org/{z}/{x}/{y}.png" },
                    SourceName = "OpenStreetMap",
                    Description = "© [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)"
                };
            }
        }

        public static readonly DependencyProperty MinZoomLevelProperty = DependencyProperty.Register(
            nameof(MinZoomLevel), typeof(int), typeof(MapTileLayer), new PropertyMetadata(0));

        public static readonly DependencyProperty MaxZoomLevelProperty = DependencyProperty.Register(
            nameof(MaxZoomLevel), typeof(int), typeof(MapTileLayer), new PropertyMetadata(19));

        public MapTileLayer()
            : this(new TileImageLoader())
        {
        }

        public MapTileLayer(ITileImageLoader tileImageLoader)
            : base(tileImageLoader)
        {
        }

        public TileMatrix TileMatrix { get; private set; }

        public IReadOnlyCollection<Tile> Tiles { get; private set; } = new List<Tile>();

        /// <summary>
        /// Minimum zoom level supported by the MapTileLayer. Default value is 0.
        /// </summary>
        public int MinZoomLevel
        {
            get { return (int)GetValue(MinZoomLevelProperty); }
            set { SetValue(MinZoomLevelProperty, value); }
        }

        /// <summary>
        /// Maximum zoom level supported by the MapTileLayer. Default value is 19.
        /// </summary>
        public int MaxZoomLevel
        {
            get { return (int)GetValue(MaxZoomLevelProperty); }
            set { SetValue(MaxZoomLevelProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            foreach (var tile in Tiles)
            {
                tile.Image.Measure(availableSize);
            }

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (TileMatrix != null)
            {
                foreach (var tile in Tiles)
                {
                    var tileSize = TileSize << (TileMatrix.ZoomLevel - tile.ZoomLevel);
                    var x = tileSize * tile.X - TileSize * TileMatrix.XMin;
                    var y = tileSize * tile.Y - TileSize * TileMatrix.YMin;

                    tile.Image.Width = tileSize;
                    tile.Image.Height = tileSize;
                    tile.Image.Arrange(new Rect(x, y, tileSize, tileSize));
                }
            }

            return finalSize;
        }

        protected override Task UpdateTileLayer()
        {
            var update = false;

            if (ParentMap == null || ParentMap.MapProjection.Type != MapProjectionType.WebMercator)
            {
                update = TileMatrix != null;
                TileMatrix = null;
            }
            else
            {
                if (TileSource != TileImageLoader.TileSource)
                {
                    if (Tiles.Count > 0)
                    {
                        Tiles = new List<Tile>(); // clear all
                    }
                    update = true;
                }

                if (SetTileMatrix())
                {
                    SetRenderTransform();
                    update = true;
                }
            }

            if (update)
            {
                UpdateTiles();

                return TileImageLoader.LoadTiles(Tiles, TileSource, SourceName);
            }

            return Task.CompletedTask;
        }

        protected override void SetRenderTransform()
        {
            // tile matrix origin in pixels
            //
            var tileMatrixOrigin = new Point(TileSize * TileMatrix.XMin, TileSize * TileMatrix.YMin);

            var tileMatrixScale = ViewTransform.ZoomLevelToScale(TileMatrix.ZoomLevel);

            ((MatrixTransform)RenderTransform).Matrix =
                ParentMap.ViewTransform.GetTileLayerTransform(tileMatrixScale, MapTopLeft, tileMatrixOrigin);
        }

        private bool SetTileMatrix()
        {
            var tileMatrixZoomLevel = (int)Math.Floor(ParentMap.ZoomLevel + 0.001); // avoid rounding issues

            var tileMatrixScale = ViewTransform.ZoomLevelToScale(tileMatrixZoomLevel);

            // bounds in tile pixels from view size
            //
            var bounds = ParentMap.ViewTransform.GetTileMatrixBounds(tileMatrixScale, MapTopLeft, ParentMap.RenderSize);

            // tile column and row index bounds
            //
            var xMin = (int)Math.Floor(bounds.X / TileSize);
            var yMin = (int)Math.Floor(bounds.Y / TileSize);
            var xMax = (int)Math.Floor((bounds.X + bounds.Width) / TileSize);
            var yMax = (int)Math.Floor((bounds.Y + bounds.Height) / TileSize);

            if (TileMatrix != null &&
                TileMatrix.ZoomLevel == tileMatrixZoomLevel &&
                TileMatrix.XMin == xMin && TileMatrix.YMin == yMin &&
                TileMatrix.XMax == xMax && TileMatrix.YMax == yMax)
            {
                return false;
            }

            TileMatrix = new TileMatrix(tileMatrixZoomLevel, xMin, yMin, xMax, yMax);

            return true;
        }

        private void UpdateTiles()
        {
            var newTiles = new List<Tile>();
            int maxZoomLevel;

            if (TileSource != null &&
                TileMatrix != null &&
                (maxZoomLevel = Math.Min(TileMatrix.ZoomLevel, MaxZoomLevel)) >= MinZoomLevel)
            {
                var minZoomLevel = IsBaseMapLayer
                    ? Math.Max(TileMatrix.ZoomLevel - MaxBackgroundLevels, MinZoomLevel)
                    : maxZoomLevel;

                for (var z = minZoomLevel; z <= maxZoomLevel; z++)
                {
                    var tileSize = 1 << (TileMatrix.ZoomLevel - z);
                    var x1 = (int)Math.Floor((double)TileMatrix.XMin / tileSize); // may be negative
                    var x2 = TileMatrix.XMax / tileSize;
                    var y1 = Math.Max(TileMatrix.YMin / tileSize, 0);
                    var y2 = Math.Min(TileMatrix.YMax / tileSize, (1 << z) - 1);

                    for (var y = y1; y <= y2; y++)
                    {
                        for (var x = x1; x <= x2; x++)
                        {
                            var tile = Tiles.FirstOrDefault(t => t.ZoomLevel == z && t.X == x && t.Y == y);

                            if (tile == null)
                            {
                                tile = new Tile(z, x, y);

                                var equivalentTile = Tiles.FirstOrDefault(
                                    t => t.ZoomLevel == z && t.XIndex == tile.XIndex && t.Y == y && !t.Pending);

                                if (equivalentTile != null)
                                {
                                    tile.SetImage(equivalentTile.Image.Source, false); // no fade-in animation
                                }
                            }

                            newTiles.Add(tile);
                        }
                    }
                }
            }

            Tiles = newTiles;

            Children.Clear();

            foreach (var tile in Tiles)
            {
                Children.Add(tile.Image);
            }
        }
    }
}
