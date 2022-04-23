// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;

namespace J4JSoftware.XamlMapControl;

public struct Vector
{
    #region operator overloads

    public static implicit operator Windows.Foundation.Point(Vector v) => new(v.X, v.Y);
    public static implicit operator Vector(Windows.Foundation.Point v) => new(v.X, v.Y);
    public static explicit operator Vector(Point p) => new(p.X, p.Y);
    public static Vector operator -(Vector v) => new(-v.X, -v.Y);
    public static Point operator +(Vector v, Point p) => new(v.X + p.X, v.Y + p.Y);
    public static Vector operator +(Vector v1, Vector v2) => new(v1.X + v2.X, v1.Y + v2.Y);
    public static Vector operator -(Vector v1, Vector v2) => new(v1.X - v2.X, v1.Y - v2.Y);
    public static Vector operator *(double f, Vector v) => new(f * v.X, f * v.Y);
    public static Vector operator *(Vector v, double f) => new(f * v.X, f * v.Y);

    public static bool operator ==(Vector v1, Vector v2) =>
        Math.Abs( v1.X - v2.X ) < XamlMapControlConstants.VectorTolerance
     && Math.Abs( v1.Y - v2.Y ) < XamlMapControlConstants.VectorTolerance;

    public static bool operator !=(Vector v1, Vector v2) => !(v1 == v2);

    #endregion

    public Vector(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; set; }
    public double Y { get; set; }

    public override bool Equals(object? o) => o is Vector vector && this == vector;
    public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
}