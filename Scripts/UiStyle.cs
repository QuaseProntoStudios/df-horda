using Godot;

namespace DeadFrequencyHorda;

/// <summary>Cores e helpers da interface: verde fósforo de CRT, igual ao Dead Frequency.</summary>
public static class UiStyle
{
    public const int FontSize = Fonts.BodySize;

    public static readonly Color Green = new(0.2f, 1f, 0.4f);
    public static readonly Color GreenDim = new(0.22f, 0.62f, 0.33f);
    public static readonly Color Hint = new(0.62f, 0.92f, 0.7f);
    public static readonly Color SelectBg = new(0.05f, 0.2f, 0.09f);
    public static readonly Color Disabled = new(0.52f, 0.58f, 0.53f);
    public static readonly Color Dark = new(0.02f, 0.06f, 0.03f);
    public static readonly Color Red = new(1f, 0.25f, 0.2f);

    public static Label MakeLabel(string text, Color color)
    {
        var label = new Label { Text = text };
        Fonts.Init();
        label.AddThemeFontOverride("font", Fonts.Regular);
        label.AddThemeFontSizeOverride("font_size", FontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 2);
        label.AddThemeConstantOverride("line_spacing", 0);
        return label;
    }

    public static Label MakeTitle(string text, Color color, int size)
    {
        Label label = MakeLabel(text, color);
        label.AddThemeFontOverride("font", Fonts.Bold);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeConstantOverride("outline_size", 3);
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
