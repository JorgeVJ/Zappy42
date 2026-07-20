using Godot;

public partial class SpeedControlPanel : Control
{
    [Signal]
    public delegate void SpeedChangedEventHandler(int t);

    [Export]
    private HSlider _slider;
    [Export]
    private Label _valueLabel;

    public override void _Ready()
    {
        _slider.ValueChanged += OnValueChanged;
    }

    private void OnValueChanged(double value)
    {
        int t = (int)value;
        _valueLabel.Text = $"{t}x";
        EmitSignal(SignalName.SpeedChanged, t);
    }

    public void SetDisplayValue(int t)
    {
        _slider.Value = Mathf.Clamp(t, _slider.MinValue, _slider.MaxValue);
        _valueLabel.Text = $"{t}x";
    }
}
