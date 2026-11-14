using Godot;

namespace DeadFrequencyHorda;

/// <summary>
/// Configurações salvas em user://settings.cfg: volumes por grupo de som, tremor de tela,
/// tela cheia e a última dificuldade. Cria os barramentos de áudio na primeira chamada.
/// </summary>
public static class GameSettings
{
    private const string FilePath = "user://settings.cfg";

    public const string BusMaster = "Master";
    public const string BusMusic = "Music";
    public const string BusSfx = "SFX";
    public const string BusEnemy = "Enemy";
    public const string BusUi = "UI";

    public static float Master = 0.8f;
    public static float Music = 0.35f;
    public static float Sfx = 0.7f;
    public static float Enemy = 0.5f;
    public static float Ui = 0.7f;
    public static bool Shake = true;
    public static bool Fullscreen;
    public static int Difficulty = 1;

    private static bool _initialized;

    /// <summary>Pode ser chamado de qualquer cena; só age na primeira vez.</summary>
    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        Load();
        EnsureBus(BusMusic);
        EnsureBus(BusSfx);
        EnsureBus(BusEnemy);
        EnsureBus(BusUi);
        ApplyAudio();
        if (Fullscreen) ApplyWindow();
        Session.Difficulty = Difficulty;
    }

    public static void Load()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(FilePath) != Error.Ok) return;

        Master = Volume(cfg, "master", Master);
        Music = Volume(cfg, "music", Music);
        Sfx = Volume(cfg, "sfx", Sfx);
        Enemy = Volume(cfg, "enemy", Enemy);
        Ui = Volume(cfg, "ui", Ui);
        Shake = cfg.GetValue("game", "shake", Shake).AsBool();
        Fullscreen = cfg.GetValue("game", "fullscreen", Fullscreen).AsBool();
        Difficulty = Mathf.Clamp(cfg.GetValue("game", "difficulty", Difficulty).AsInt32(), 0, Difficulties.All.Length - 1);
    }

    public static void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("audio", "master", Master);
        cfg.SetValue("audio", "music", Music);
        cfg.SetValue("audio", "sfx", Sfx);
        cfg.SetValue("audio", "enemy", Enemy);
        cfg.SetValue("audio", "ui", Ui);
        cfg.SetValue("game", "shake", Shake);
        cfg.SetValue("game", "fullscreen", Fullscreen);
        cfg.SetValue("game", "difficulty", Difficulty);
        cfg.Save(FilePath);
    }

    public static void ApplyAudio()
    {
        SetBus(BusMaster, Master);
        SetBus(BusMusic, Music);
        SetBus(BusSfx, Sfx);
        SetBus(BusEnemy, Enemy);
        SetBus(BusUi, Ui);
    }

    public static void ApplyWindow()
    {
        DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
    }

    private static float Volume(ConfigFile cfg, string key, float fallback)
    {
        return Mathf.Clamp(cfg.GetValue("audio", key, fallback).AsSingle(), 0f, 1f);
    }

    private static void EnsureBus(string name)
    {
        if (AudioServer.GetBusIndex(name) >= 0) return;

        int index = AudioServer.BusCount;
        AudioServer.AddBus(index);
        AudioServer.SetBusName(index, name);
        AudioServer.SetBusSend(index, BusMaster);
    }

    private static void SetBus(string name, float linear)
    {
        int index = AudioServer.GetBusIndex(name);
        if (index < 0) return;

        AudioServer.SetBusMute(index, linear <= 0.001f);
        AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Mathf.Max(linear, 0.0001f)));
    }
}
