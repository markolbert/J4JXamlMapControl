﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Globalization;
using Microsoft.UI.Xaml;

namespace J4JSoftware.XamlMapControl
{
    /// <summary>
    /// Draws a graticule overlay.
    /// </summary>
    public partial class MapGraticule : MapOverlay
    {
        public static readonly DependencyProperty MinLineDistanceProperty = DependencyProperty.Register(
            nameof(MinLineDistance), typeof(double), typeof(MapGraticule), new PropertyMetadata(150d));

        /// <summary>
        /// Minimum graticule line distance in pixels. The default value is 150.
        /// </summary>
        public double MinLineDistance
        {
            get { return (double)GetValue(MinLineDistanceProperty); }
            set { SetValue(MinLineDistanceProperty, value); }
        }

        private double GetLineDistance()
        {
            var minDistance = MinLineDistance / PixelPerLongitudeDegree(ParentMap.Center);
            var scale = minDistance < 1d / 60d ? 3600d : minDistance < 1d ? 60d : 1d;
            minDistance *= scale;

            var lineDistances = new double[] { 1d, 2d, 5d, 10d, 15d, 30d, 60d };
            var i = 0;

            while (i < lineDistances.Length - 1 && lineDistances[i] < minDistance)
            {
                i++;
            }

            return Math.Min(lineDistances[i] / scale, 30d);
        }

        private double PixelPerLongitudeDegree(Location location)
        {
            return Math.Max(1d, // a reasonable lower limit
                ParentMap.GetScale(location).X *
                Math.Cos(location.Latitude * Math.PI / 180d) * MapProjection.Wgs84MeterPerDegree);
        }

        private static string GetLabelFormat(double lineDistance)
        {
            return lineDistance < 1d / 60d ? "{0} {1}°{2:00}'{3:00}\""
                 : lineDistance < 1d ? "{0} {1}°{2:00}'" : "{0} {1}°";
        }

        private static string GetLabelText(double value, string format, string hemispheres)
        {
            var hemisphere = hemispheres[0];

            value = (value + 540d) % 360d - 180d;

            if (value < -1e-8) // ~1mm
            {
                value = -value;
                hemisphere = hemispheres[1];
            }

            var seconds = (int)Math.Round(value * 3600d);

            return string.Format(CultureInfo.InvariantCulture,
                format, hemisphere, seconds / 3600, seconds / 60 % 60, seconds % 60);
        }
    }
}
