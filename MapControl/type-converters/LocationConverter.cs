using System;
using System.ComponentModel;
using System.Globalization;

namespace J4JSoftware.XamlMapControl;

public class LocationConverter : TypeConverter
{
    public override bool CanConvertFrom( ITypeDescriptorContext? context, Type sourceType ) =>
        sourceType == typeof( string );

    public override object ConvertFrom( ITypeDescriptorContext? context, CultureInfo? culture, object value ) =>
        Location.Parse( (string) value );
}
