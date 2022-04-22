// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.ComponentModel;
using System.Globalization;

namespace J4JSoftware.XamlMapControl;

public class TileSourceConverter : TypeConverter
{
    public override bool CanConvertFrom( ITypeDescriptorContext? context, Type sourceType ) =>
        sourceType == typeof( string );

    public override object ConvertFrom( ITypeDescriptorContext? context, CultureInfo? culture, object value ) =>
        new TileSource { UriFormat = (string) value };
}