using Godot;

namespace DeadFrequencyHorda;

/// <summary>Fonte pixel art do jogo (Pixelify Sans, OFL), sem suavização para os pixels ficarem nítidos.</summary>
public static class Fonts
{
    public const int BodySize = 10;

    public static Font Regular { get; private set; }
    public static Font Bold { get; private set; }
    private static bool _ready;

    public static void Init()
    {
        if (_ready) return;
        _ready = true;

        Regular = Load("res://Fonts/PixelifySans-Medium.ttf") ?? ThemeDB.FallbackFont;
        Bold = Load("res://Fonts/PixelifySans-Bold.ttf") ?? Regular;
        ThemeDB.FallbackFont = Regular;
        ThemeDB.FallbackFontSize = BodySize;
    }

    private static Font Load(string path)
    {
        var font = ResourceLoader.Load<FontFile>(path);
        if (font == null) return null;

        font.Antialiasing = TextServer.FontAntialiasing.None;
        font.SubpixelPositioning = TextServer.SubpixelPositioning.Disabled;
        font.Hinting = TextServer.Hinting.None;
        font.GenerateMipmaps = false;
        return font;
    }
}
