using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>Carrega texturas e sons pelo nome, com cache. Arquivo faltando não derruba o jogo.</summary>
public static class Assets
{
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static readonly Dictionary<string, AudioStream> Streams = new();

    public static Texture2D Texture(string name)
    {
        if (Textures.TryGetValue(name, out Texture2D cached)) return cached;

        string path = $"res://Art/{name}.png";
        Texture2D texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        if (texture == null)
        {
            GD.PushWarning($"Textura não encontrada: {path}");
            texture = Placeholder();
        }
        Textures[name] = texture;
        return texture;
    }

    /// <summary>path relativo a res://Audio/, por exemplo "SFX/impact_box.wav".</summary>
    public static AudioStream Stream(string relativePath)
    {
        if (Streams.TryGetValue(relativePath, out AudioStream cached)) return cached;

        string path = $"res://Audio/{relativePath}";
        AudioStream stream = ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;
        if (stream == null) GD.PushWarning($"Som não encontrado: {path}");
        Streams[relativePath] = stream;
        return stream;
    }

    private static Texture2D Placeholder()
    {
        Image image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8);
        image.Fill(new Color(1f, 0f, 1f));
        return ImageTexture.CreateFromImage(image);
    }
}
