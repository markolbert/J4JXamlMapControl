// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using Windows.UI.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace J4JSoftware.XamlMapControl;

/// <summary>
/// Base class for map overlays with background, foreground, stroke and font properties.
/// </summary>
public class MapOverlay : MapPanel
{
    #region FontFamily property

    public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
        nameof(FontFamily), typeof(FontFamily), typeof(MapOverlay), new PropertyMetadata(null));

    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    #endregion

    #region FontSize property

    public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
        nameof(FontSize), typeof(double), typeof(MapOverlay), new PropertyMetadata(12d));

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    #endregion

    #region FontStyle property

    public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register(
        nameof(FontStyle), typeof(FontStyle), typeof(MapOverlay), new PropertyMetadata(FontStyle.Normal));

    public FontStyle FontStyle
    {
        get => (FontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    #endregion

    #region FontStretch property

    public static readonly DependencyProperty FontStretchProperty = DependencyProperty.Register(
        nameof(FontStretch), typeof(FontStretch), typeof(MapOverlay), new PropertyMetadata(FontStretch.Normal));

    public FontStretch FontStretch
    {
        get => (FontStretch)GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    #endregion

    #region FontWeight property

    public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
        nameof(FontWeight), typeof(FontWeight), typeof(MapOverlay), new PropertyMetadata(FontWeights.Normal));

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    #endregion

    #region Foreground property

    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
        nameof(Foreground), typeof(Brush), typeof(MapOverlay), new PropertyMetadata(null));

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    #endregion

    #region Stroke property

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke), typeof(Brush), typeof(MapOverlay), new PropertyMetadata(null));

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    #endregion

    #region StrokeThickness property

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(MapOverlay), new PropertyMetadata(1d));

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    #endregion

    #region StrokeDashArray property

    public static readonly DependencyProperty StrokeDashArrayProperty = DependencyProperty.Register(
        nameof(StrokeDashArray), typeof(DoubleCollection), typeof(MapOverlay), new PropertyMetadata(null));

    public DoubleCollection StrokeDashArray
    {
        get => (DoubleCollection)GetValue(StrokeDashArrayProperty);
        set => SetValue(StrokeDashArrayProperty, value);
    }

    #endregion

    #region StrokeDashOffset property

    public static readonly DependencyProperty StrokeDashOffsetProperty = DependencyProperty.Register(
        nameof(StrokeDashOffset), typeof(double), typeof(MapOverlay), new PropertyMetadata(0d));

    public double StrokeDashOffset
    {
        get => (double)GetValue(StrokeDashOffsetProperty);
        set => SetValue(StrokeDashOffsetProperty, value);
    }

    #endregion

    #region StrokeDashCap property

    public static readonly DependencyProperty StrokeDashCapProperty = DependencyProperty.Register(
        nameof(StrokeDashCap), typeof(PenLineCap), typeof(MapOverlay), new PropertyMetadata(PenLineCap.Flat));

    public PenLineCap StrokeDashCap
    {
        get => (PenLineCap)GetValue(StrokeDashCapProperty);
        set => SetValue(StrokeDashCapProperty, value);
    }

    #endregion

    #region StrokeStartLineCap property

    public static readonly DependencyProperty StrokeStartLineCapProperty = DependencyProperty.Register(
        nameof(StrokeStartLineCap), typeof(PenLineCap), typeof(MapOverlay), new PropertyMetadata(PenLineCap.Flat));

    public PenLineCap StrokeStartLineCap
    {
        get => (PenLineCap)GetValue(StrokeStartLineCapProperty);
        set => SetValue(StrokeStartLineCapProperty, value);
    }

    #endregion

    #region StrokeEndLineCap property

    public static readonly DependencyProperty StrokeEndLineCapProperty = DependencyProperty.Register(
        nameof(StrokeEndLineCap), typeof(PenLineCap), typeof(MapOverlay), new PropertyMetadata(PenLineCap.Flat));

    public PenLineCap StrokeEndLineCap
    {
        get => (PenLineCap)GetValue(StrokeEndLineCapProperty);
        set => SetValue(StrokeEndLineCapProperty, value);
    }

    #endregion

    #region StrokeLineJoin property

    public static readonly DependencyProperty StrokeLineJoinProperty = DependencyProperty.Register(
        nameof(StrokeLineJoin), typeof(PenLineJoin), typeof(MapOverlay), new PropertyMetadata(PenLineJoin.Miter));

    public PenLineJoin StrokeLineJoin
    {
        get => (PenLineJoin)GetValue(StrokeLineJoinProperty);
        set => SetValue(StrokeLineJoinProperty, value);
    }

    #endregion

    #region StrokeMiterLimit property

    public static readonly DependencyProperty StrokeMiterLimitProperty = DependencyProperty.Register(
        nameof(StrokeMiterLimit), typeof(double), typeof(MapOverlay), new PropertyMetadata(1d));

    public double StrokeMiterLimit
    {
        get => (double)GetValue(StrokeMiterLimitProperty);
        set => SetValue(StrokeMiterLimitProperty, value);
    }

    #endregion

    public MapOverlay()
    {
        IsHitTestVisible = false;
    }

    protected override void SetParentMap(MapBase? map)
    {
        if (map != null)
        {
            // If this.Forground is not explicitly set, bind it to map.Foreground
            this.SetBindingOnUnsetProperty(ForegroundProperty, map, MapBase.ForegroundProperty, nameof(Foreground));

            // If this.Stroke is not explicitly set, bind it to this.Foreground
            this.SetBindingOnUnsetProperty(StrokeProperty, this, ForegroundProperty, nameof(Foreground));
        }

        base.SetParentMap(map);
    }
}