using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Container class for an item in a MapItemsControl.
/// </summary>
public class MapItem : ListBoxItem
{
    #region LocationMemberPath property

    /// <summary>
    /// Path to a source property for binding the Location property.
    /// </summary>
    public static readonly DependencyProperty LocationMemberPathProperty = DependencyProperty.Register(
        nameof(LocationMemberPath), typeof(string), typeof(MapItem),
        new PropertyMetadata(null, (o, e) => BindingOperations.SetBinding(
                                 o, LocationProperty, new Binding { Path = new PropertyPath((string)e.NewValue) })));

    public string LocationMemberPath
    {
        get => (string)GetValue(LocationMemberPathProperty);
        set => SetValue(LocationMemberPathProperty, value);
    }

    #endregion

    #region AutoCollapse property

    /// <summary>
    /// Gets/sets MapPanel.AutoCollapse.
    /// </summary>
    public static readonly DependencyProperty AutoCollapseProperty = DependencyProperty.Register(
        nameof(AutoCollapse), typeof(bool), typeof(MapItem),
        new PropertyMetadata(false, (o, e) => MapPanel.SetAutoCollapse((MapItem)o, (bool)e.NewValue)));

    public bool AutoCollapse
    {
        get => (bool)GetValue(AutoCollapseProperty);
        set => SetValue(AutoCollapseProperty, value);
    }

    #endregion

    #region LocationProperty

    /// <summary>
    /// Gets/sets MapPanel.Location.
    /// </summary>
    public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(
        nameof(Location), typeof(Location), typeof(MapItem),
        new PropertyMetadata(null, (o, e) => MapPanel.SetLocation((MapItem)o, (Location)e.NewValue)));

    public Location Location
    {
        get => (Location)GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    #endregion

    public MapItem()
    {
        DefaultStyleKey = typeof(MapItem);
        J4JSoftware.XamlMapControl.MapPanel.InitMapElement(this);
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        (ItemsControl.ItemsControlFromItemContainer(this) as MapItemsControl)?.OnItemClicked(
            this, e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control), e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift));
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        var parentMap = MapPanel.GetParentMap(this);

        if (parentMap == null)
            return;

        // If this.Background is not explicitly set, bind it to parentMap.Background
        this.SetBindingOnUnsetProperty(BackgroundProperty, parentMap, Panel.BackgroundProperty, nameof(Background));

        // If this.Foreground is not explicitly set, bind it to parentMap.Foreground
        this.SetBindingOnUnsetProperty(ForegroundProperty, parentMap, MapBase.ForegroundProperty, nameof(Foreground));

        // If this.BorderBrush is not explicitly set, bind it to parentMap.Foreground
        this.SetBindingOnUnsetProperty(BorderBrushProperty, parentMap, MapBase.ForegroundProperty, nameof(Foreground));
    }

}
