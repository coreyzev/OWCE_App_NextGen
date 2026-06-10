namespace OWCE.Views.Controls;

public partial class BatteryView : ContentView
{
    public static readonly BindableProperty PercentProperty =
        BindableProperty.Create(nameof(Percent), typeof(int), typeof(BatteryView), 0,
            propertyChanged: (b, _, n) => ((BatteryView)b).UpdateDisplay((int)n));

    public int Percent
    {
        get => (int)GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }

    public BatteryView()
    {
        InitializeComponent();
    }

    private void UpdateDisplay(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        PercentLabel.Text = $"{percent}%";

        // Fill width proportional to percentage (max 28dp)
        FillBox.WidthRequest = 28 * (percent / 100.0);

        FillBox.Color = percent switch
        {
            > 50 => Color.FromArgb("#2ECC71"),  // Green
            > 20 => Color.FromArgb("#F39C12"),  // Yellow
            _    => Color.FromArgb("#E74C3C"),  // Red
        };
    }
}
