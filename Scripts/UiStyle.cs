using Godot;

namespace DeadFrequencyHorda;

/// <summary>Cores e helpers da interface: verde fósforo de CRT, igual ao Dead Frequency.</summary>
public static class UiStyle
{
    public const int FontSize = 8;

    public static readonly Color Green = new(0.2f, 1f, 0.4f);
    public static readonly Color GreenDim = new(0.1f, 0.45f, 0.2f);
    public static readonly Color Hint = new(0.45f, 0.8f, 0.55f);
    public static readonly Color SelectBg = new(0.05f, 0.2f, 0.09f);
    public static readonly Color Disabled = new(0.35f, 0.4f, 0.36f);
    public static readonly Color Dark = new(0.02f, 0.06f, 0.03f);
    public static readonly Color Red = new(1f, 0.25f, 0.2f);

    public static Label MakeLabel(string text, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", FontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 2);
        label.AddThemeConstantOverride("line_spacing", 0);
        return label;
    }

    public static StyleBoxFlat Box(Color background, Color border, int borderWidth = 1)
    {
        var box = new StyleBoxFlat { BgColor = background, BorderColor = border };
        box.SetBorderWidthAll(borderWidth);
        box.SetContentMarginAll(4f);
        return box;
    }
}
