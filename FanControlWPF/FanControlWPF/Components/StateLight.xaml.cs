using System.Windows;
using System.Windows.Controls;
using FanControlWPF.Enum;

namespace FanControlWPF.Components;

/// <summary>
/// StateLight.xaml 的互動邏輯
/// </summary>
public partial class StateLight: UserControl
{
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        "Size", typeof(double), typeof(StateLight), new PropertyMetadata(50.0));

    public static readonly DependencyProperty LightColorProperty = DependencyProperty.Register(
        "LightColor", typeof(TrafficLightColor), typeof(StateLight), new PropertyMetadata(TrafficLightColor.Red));
    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public TrafficLightColor LightColor
    {
        get => (TrafficLightColor)GetValue(LightColorProperty);
        set => SetValue(LightColorProperty, value);
    }
    public StateLight()
    {
        InitializeComponent();
        DataContext = this;
    }
}