// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// ContentControl placed on a MapPanel at a geographic location specified by the Location property.
/// </summary>
public class MapContentControl : ContentControl
{
    #region AutoCollapse property

    /// <summary>
    /// Gets/sets MapPanel.AutoCollapse.
    /// </summary>
    public static readonly DependencyProperty AutoCollapseProperty = DependencyProperty.Register(
        nameof(AutoCollapse), typeof(bool), typeof(MapContentControl),
        new PropertyMetadata(false, (o, e) => MapPanel.SetAutoCollapse((MapContentControl)o, (bool)e.NewValue)));

    public bool AutoCollapse
    {
        get => (bool)GetValue(AutoCollapseProperty);
        set => SetValue(AutoCollapseProperty, value);
    }

    #endregion

    #region Location property

    /// <summary>
    /// Gets/sets MapPanel.Location.
    /// </summary>
    public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(
        nameof(Location), typeof(Location), typeof(MapContentControl),
        new PropertyMetadata(null, (o, e) => MapPanel.SetLocation((MapContentControl)o, (Location)e.NewValue)));

    public Location Location
    {
        get => (Location)GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    #endregion

    public MapContentControl()
    {
        DefaultStyleKey = typeof(MapContentControl);
        MapPanel.InitMapElement(this);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        var parentMap = MapPanel.GetParentMap(this);

        if( parentMap == null )
            return;

        // If this.Background is not explicitly set, bind it to parentMap.Background
        this.SetBindingOnUnsetProperty(BackgroundProperty, parentMap, Panel.BackgroundProperty, nameof(Background));

        // If this.Foreground is not explicitly set, bind it to parentMap.Foreground
        this.SetBindingOnUnsetProperty(ForegroundProperty, parentMap, MapBase.ForegroundProperty, nameof(Foreground));

        // If this.BorderBrush is not explicitly set, bind it to parentMap.Foreground
        this.SetBindingOnUnsetProperty(BorderBrushProperty, parentMap, MapBase.ForegroundProperty, nameof(Foreground));
    }
}