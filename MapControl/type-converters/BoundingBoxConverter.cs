﻿using System;
using System.ComponentModel;
using System.Globalization;

namespace J4JSoftware.XamlMapControl;

public class BoundingBoxConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return BoundingBox.Parse((string)value);
    }
}
