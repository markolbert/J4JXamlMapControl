using System;
using System.ComponentModel;
using System.Globalization;

namespace J4JSoftware.XamlMapControl;

public class LocationCollectionConverter : TypeConverter
{
    public override bool CanConvertFrom( ITypeDescriptorContext? context, Type sourceType ) =>
        sourceType == typeof( string );

    public override object ConvertFrom( ITypeDescriptorContext? context, CultureInfo? culture, object value ) =>
        LocationCollection.Parse( (string) value );
}
