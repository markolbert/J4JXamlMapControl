using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl;

public interface IMapLayer : IMapElement
{
    Brush? MapBackground { get; }
    Brush? MapForeground { get; }
}
