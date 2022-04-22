// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.ComponentModel;
using System.Globalization;
// ReSharper disable ValueParameterNotUsed

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// A geographic bounding box with south and north latitude and west and east longitude values in degrees.
/// </summary>
[TypeConverter(typeof(BoundingBoxConverter))]
public class BoundingBox
{
    private double _south;
    private double _north;

    public BoundingBox()
    {
    }

    public BoundingBox(double south, double west, double north, double east)
    {
        South = south;
        West = west;
        North = north;
        East = east;
    }

    public BoundingBox(BoundingBox boundingBox, double longitudeOffset)
        : this(boundingBox.South, boundingBox.West + longitudeOffset,
               boundingBox.North, boundingBox.East + longitudeOffset)
    {
    }

    public double West { get; set; }

    public double East { get; set; }

    public double South
    {
        get => _south;
        set => _south = Math.Min(Math.Max(value, -90d), 90d);
    }

    public double North
    {
        get => _north;
        set => _north = Math.Min(Math.Max(value, -90d), 90d);
    }

    public virtual double Width
    {
        get => East - West;
        protected set { }
    }

    public virtual double Height
    {
        get => North - South;
        protected set { }
    }

    public virtual Location Center
    {
        get => new((South + North) / 2d, (West + East) / 2d);
        protected set { }
    }

    public static BoundingBox Parse(string s)
    {
        var values = s.Split(new char[] { ',' });

        if (values.Length != 4)
        {
            throw new FormatException("BoundingBox string must be a comma-separated list of four floating point numbers.");
        }

        return new BoundingBox(
            double.Parse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture),
            double.Parse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture),
            double.Parse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture),
            double.Parse(values[3], NumberStyles.Float, CultureInfo.InvariantCulture));
    }
}