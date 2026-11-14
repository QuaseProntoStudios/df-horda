using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>Um retrato da partida: o que "Salvar e sair" guarda e "Continuar" restaura.</summary>
public sealed class RunData
{
    public int Difficulty;
    public int Level;
    public int Kills;
    public int HordeIndex;
    public int EliteIndex;
    public float Time;
    public float Xp;
    public float Hp;
    public float VanX;
    public float VanY;
    public float Heading;
    public readonly Dictionary<string, int> Levels = new();
    public readonly Dictionary<string, float> Power = new();
}

/// <summary>Salva em user://run.cfg. Os inimigos não entram: a horda recomeça em volta da van.</summary>
public static class RunSave
{
    private const string FilePath = "user://run.cfg";

    public static bool Exists => FileAccess.FileExists(FilePath);

    public static void Save(RunData data)
    {
        var cfg = new ConfigFile();
        cfg.SetValue("run", "difficulty", data.Difficulty);
        cfg.SetValue("run", "level", data.Level);
        cfg.SetValue("run", "kills", data.Kills);
        cfg.SetValue("run", "horde_index", data.HordeIndex);
        cfg.SetValue("run", "elite_index", data.EliteIndex);
        cfg.SetValue("run", "time", data.Time);
        cfg.SetValue("run", "xp", data.Xp);
        cfg.SetValue("run", "hp", data.Hp);
        cfg.SetValue("run", "van_x", data.VanX);
        cfg.SetValue("run", "van_y", data.VanY);
        cfg.SetValue("run", "heading", data.Heading);
        foreach (UpgradeDef def in UpgradeCatalog.All)
        {
            cfg.SetValue("levels", def.Id, data.Levels[def.Id]);
            cfg.SetValue("power", def.Id, data.Power[def.Id]);
        }
        cfg.Save(FilePath);
    }

    /// <summary>Devolve null se não houver save ou ele estiver ilegível.</summary>
    public static RunData Load()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(FilePath) != Error.Ok) return null;
        if (!cfg.HasSectionKey("run", "time")) return null;

        var data = new RunData
        {
            Difficulty = Mathf.Clamp(cfg.GetValue("run", "difficulty", 1).AsInt32(), 0, Difficulties.All.Length - 1),
            Level = Mathf.Max(cfg.GetValue("run", "level", 1).AsInt32(), 1),
            Kills = cfg.GetValue("run", "kills", 0).AsInt32(),
            HordeIndex = cfg.GetValue("run", "horde_index", 0).AsInt32(),
            EliteIndex = cfg.GetValue("run", "elite_index", 0).AsInt32(),
            Time = cfg.GetValue("run", "time", 0f).AsSingle(),
            Xp = cfg.GetValue("run", "xp", 0f).AsSingle(),
            Hp = cfg.GetValue("run", "hp", 100f).AsSingle(),
            VanX = cfg.GetValue("run", "van_x", 0f).AsSingle(),
            VanY = cfg.GetValue("run", "van_y", 0f).AsSingle(),
            Heading = cfg.GetValue("run", "heading", -Mathf.Pi / 2f).AsSingle(),
        };
        foreach (UpgradeDef def in UpgradeCatalog.All)
        {
            data.Levels[def.Id] = cfg.GetValue("levels", def.Id, def.StartLevel).AsInt32();
            data.Power[def.Id] = cfg.GetValue("power", def.Id, (float)def.StartLevel).AsSingle();
        }
        return data;
    }

    public static void Delete()
    {
        if (Exists) DirAccess.RemoveAbsolute(FilePath);
    }

    /// <summary>Linha curta para mostrar no menu, por exemplo "Nível 12 - 04:31 - NORMAL".</summary>
    public static string Summary()
    {
        RunData data = Load();
        if (data == null) return null;

        int seconds = (int)data.Time;
        return $"Nível {data.Level} - {seconds / 60:00}:{seconds % 60:00} - {Difficulties.Get(data.Difficulty).Name}";
    }
}
