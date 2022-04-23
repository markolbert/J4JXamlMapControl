// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Replaces Windows.Foundation.Point to achieve necessary floating point precision.
/// </summary>
public struct Point
{
    #region operator overloads

    public static implicit operator Windows.Foundation.Point( Point p ) => new( p.X, p.Y );
    public static implicit operator Point(Windows.Foundation.Point p) => new(p.X, p.Y);
    public static explicit operator Point(Vector v) => new(v.X, v.Y); 
    public static Point operator -(Point p) => new(-p.X, -p.Y);
    public static Point operator +(Point p, Vector v) => new(p.X + v.X, p.Y + v.Y);
    public static Point operator -(Point p, Vector v) => new(p.X - v.X, p.Y - v.Y);
    public static Vector operator -(Point p1, Point p2) => new(p1.X - p2.X, p1.Y - p2.Y);

    public static bool operator==( Point p1, Point p2 ) =>
        Math.Abs( p1.X - p2.X ) < XamlMapControlConstants.PointTolerance
     && Math.Abs( p1.Y - p2.Y ) < XamlMapControlConstants.PointTolerance;

    public static bool operator !=(Point p1, Point p2) => !(p1 == p2);

    #endregion

    public double X { get; set; }
    public double Y { get; set; }

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object? o) => o is Point point && this == point;
    public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
}