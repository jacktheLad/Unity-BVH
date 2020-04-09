using System;
using System.Globalization;
using UnityEngine;
using ZeroFormatter;

[ZeroFormattable]
public struct Vector4Ser : IFormattable
{
    [Index(0)]
    public float x;
    [Index(1)]
    public float y;
    [Index(2)]
    public float z;
    [Index(3)]
    public float w;

    public Vector4Ser(Vector4 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
        w = v.w;
    }

    public Vector4Ser(float xx, float yy, float zz, float ww)
    {
        x = xx;
        y = yy;
        z = zz;
        w = ww;
    }

    public override string ToString()
    {
        return ToString(null, CultureInfo.InvariantCulture.NumberFormat);
    }

    public string ToString(string format)
    {
        return ToString(format, CultureInfo.InvariantCulture.NumberFormat);
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return string.Format("({0}, {1}, {2}, {3})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), z.ToString(format, formatProvider), w.ToString(format, formatProvider));
    }

    public static implicit operator Vector4(Vector4Ser vs)
    {
        return new Vector4(vs.x, vs.y, vs.z, vs.w);
    }

    public static implicit operator Vector4Ser(Vector4 v)
    {
        return new Vector4Ser(v);
    }
}