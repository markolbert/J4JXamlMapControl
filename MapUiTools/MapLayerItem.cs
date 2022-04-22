using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace J4JSoftware.XamlMapControl.MapUiTools;

[ContentProperty(Name = nameof(Layer))]
public class MapLayerItem
{
    public string? Text { get; set; }
    public UIElement? Layer { get; set; }
}
