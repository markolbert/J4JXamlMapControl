using Microsoft.UI.Xaml.Markup;

namespace J4JSoftware.XamlMapControl.MapUiTools;

[ContentProperty(Name = nameof(Projection))]
public class MapProjectionItem
{
    public string? Text { get; set; }
    public string? Projection { get; set; }
}
