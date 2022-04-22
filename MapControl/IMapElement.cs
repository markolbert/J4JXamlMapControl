namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Optional interface to hold the value of the attached property MapPanel.ParentMap.
/// </summary>
public interface IMapElement
{
    MapBase? ParentMap { get; set; }
}
