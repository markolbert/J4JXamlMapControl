﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Globalization;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace J4JSoftware.XamlMapControl
{
    /// <summary>
    /// Draws a map scale overlay.
    /// </summary>
    public class MapScale : MapOverlay
    {
        public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(
            nameof(Padding), typeof(Thickness), typeof(MapScale), new PropertyMetadata(new Thickness(4)));

        private readonly Polyline line = new Polyline();

        private readonly TextBlock label = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            TextAlignment = TextAlignment.Center
        };

        public MapScale()
        {
            MinWidth = 100d;
            Children.Add(line);
            Children.Add(label);
        }

        protected override void SetParentMap(MapBase map)
        {
            base.SetParentMap(map);

            line.SetBinding(Shape.StrokeProperty, this.GetOrCreateBinding(StrokeProperty, nameof(Stroke)));
            line.SetBinding(Shape.StrokeThicknessProperty, this.GetOrCreateBinding(StrokeThicknessProperty, nameof(StrokeThickness)));
            label.SetBinding(TextBlock.ForegroundProperty, this.GetOrCreateBinding(ForegroundProperty, nameof(Foreground)));
        }

        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = new Size();

            if (ParentMap != null)
            {
                var scale = ParentMap.GetScale(ParentMap.Center).X;
                var length = MinWidth / scale;
                var magnitude = Math.Pow(10d, Math.Floor(Math.Log10(length)));

                length = length / magnitude < 2d ? 2d * magnitude
                       : length / magnitude < 5d ? 5d * magnitude
                       : 10d * magnitude;

                size.Width = length * scale + StrokeThickness + Padding.Left + Padding.Right;
                size.Height = 1.25 * FontSize + StrokeThickness + Padding.Top + Padding.Bottom;

                var x1 = Padding.Left + StrokeThickness / 2d;
                var x2 = size.Width - Padding.Right - StrokeThickness / 2d;
                var y1 = size.Height / 2d;
                var y2 = size.Height - Padding.Bottom - StrokeThickness / 2d;

                line.Points = new PointCollection
                {
                    new Point(x1, y1),
                    new Point(x1, y2),
                    new Point(x2, y2),
                    new Point(x2, y1)
                };
                line.Measure(size);

                label.Text = length >= 1000d
                    ? string.Format(CultureInfo.InvariantCulture, "{0:0} km", length / 1000d)
                    : string.Format(CultureInfo.InvariantCulture, "{0:0} m", length);
                label.Width = size.Width;
                label.Height = size.Height;
                label.Measure(size);
            }

            return size;
        }

        protected override void OnViewportChanged(ViewportChangedEventArgs e)
        {
            InvalidateMeasure();
        }
    }
}
