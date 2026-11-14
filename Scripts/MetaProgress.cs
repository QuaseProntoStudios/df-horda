using System;
using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

public sealed class MetaDef
{
    public string Id;
    public string Name;
    public int MaxLevel;
    public float BaseCost;
    public float Growth;
    /// <summary>Efeito de cada nível (0.1 = 10% quando Percent).</summary>
    public float PerLevel;
    public bool Percent;
    /// <summary>Texto do efeito, com {0} no lugar do valor total.</summary>
    public string Text;

    public int CostAt(int level)
    {
        return Mathf.Max(1, Mathf.RoundToInt(BaseCost * Mathf.Pow(Growth, level)));
    }

    public string Describe(int level)
    {
        float total = PerLevel * level;
        string shown = Percent ? $"{total * 100f:0.#}" : $"{total:0.#}";
        return Text.Replace("{0}", shown);
    }
}

/// <summary>
/// Progressão entre partidas: as fichas da 96.6 ganhas ao fim de cada partida compram
/// upgrades permanentes na Oficina. Salvo em user://meta.cfg.
/// </summary>
public static class MetaProgress
{
    private const string FilePath = "user://meta.cfg";

    public const string Chassi = "chassi";
    public const string Motor = "motor";
    public const string Calibre = "calibre";
    public const string Ima = "ima";
    public const string Antena = "antena";
    public const string Sorte = "sorte";
    public const string Mecanico = "mecanico";
    public const string Arranque = "arranque";
    public const string Revive = "revive";

    public static readonly MetaDef[] All =
    {
        new MetaDef { Id = Chassi, Name = "Chassi Reforçado", MaxLevel = 10, BaseCost = 12, Growth = 1.3f, PerLevel = 10f, Text = "+{0} de vida máxima" },
        new MetaDef { Id = Motor, Name = "Motor Tunado", MaxLevel = 8, BaseCost = 15, Growth = 1.3f, PerLevel = 0.03f, Percent = true, Text = "+{0}% de velocidade" },
        new MetaDef { Id = Calibre, Name = "Calibre Maior", MaxLevel = 10, BaseCost = 18, Growth = 1.35f, PerLevel = 0.04f, Percent = true, Text = "+{0}% de dano das armas" },
        new MetaDef { Id = Ima, Name = "Ímã de Fábrica", MaxLevel = 8, BaseCost = 10, Growth = 1.3f, PerLevel = 0.08f, Percent = true, Text = "+{0}% de alcance do ímã" },
        new MetaDef { Id = Antena, Name = "Antena Melhor", MaxLevel = 8, BaseCost = 20, Growth = 1.35f, PerLevel = 0.05f, Percent = true, Text = "+{0}% de sinais (XP)" },
        new MetaDef { Id = Sorte, Name = "Amuleto da Rádio", MaxLevel = 8, BaseCost = 25, Growth = 1.4f, PerLevel = 0.04f, Percent = true, Text = "+{0}% de chance de raridade melhor" },
        new MetaDef { Id = Mecanico, Name = "Mecânico de Plantão", MaxLevel = 5, BaseCost = 20, Growth = 1.4f, PerLevel = 0.15f, Percent = true, Text = "+{0}% de chance de kit de reparo" },
        new MetaDef { Id = Arranque, Name = "Largada Lançada", MaxLevel = 3, BaseCost = 60, Growth = 2f, PerLevel = 1f, Text = "Começa {0} nível(is) acima" },
        new MetaDef { Id = Revive, Name = "Segunda Chance", MaxLevel = 2, BaseCost = 100, Growth = 2.5f, PerLevel = 1f, Text = "Revive {0}x com 40% de vida" },
    };

    private static readonly Dictionary<string, int> _levels = new();
    private static int _fichas;
    private static int _totalEarned;
    private static bool _loaded;

    public static int Fichas
    {
        get { EnsureLoaded(); return _fichas; }
    }

    public static int TotalEarned
    {
        get { EnsureLoaded(); return _totalEarned; }
    }

    public static int Level(string id)
    {
        EnsureLoaded();
        return _levels.TryGetValue(id, out int level) ? level : 0;
    }

    /// <summary>Efeito total do upgrade no nível atual (0 se nunca comprado).</summary>
    public static float Value(string id)
    {
        foreach (MetaDef def in All)
        {
            if (def.Id == id) return def.PerLevel * Level(id);
        }
        return 0f;
    }

    public static int TotalLevels()
    {
        EnsureLoaded();
        int total = 0;
        foreach (MetaDef def in All) total += Level(def.Id);
        return total;
    }

    public static void AddFichas(int amount, bool countsAsEarned = true)
    {
        EnsureLoaded();
        if (amount <= 0) return;
        _fichas += amount;
        if (countsAsEarned) _totalEarned += amount;
        Save();
    }

    public static bool CanBuy(MetaDef def)
    {
        return Level(def.Id) < def.MaxLevel && Fichas >= def.CostAt(Level(def.Id));
    }

    public static bool Buy(MetaDef def)
    {
        if (!CanBuy(def)) return false;

        _fichas -= def.CostAt(Level(def.Id));
        _levels[def.Id] = Level(def.Id) + 1;
        Save();

        Achievements.Unlock("oficina");
        if (TotalLevels() >= 10) Achievements.Unlock("cliente_fiel");
        if (_levels[def.Id] >= def.MaxLevel) Achievements.Unlock("mecanico");
        return true;
    }

    /// <summary>Fichas de uma partida: tempo, abates, nível e bônus de chefe, vezes a dificuldade.</summary>
    public static int RunReward(float seconds, int kills, int level, bool bossKilled, DifficultyDef difficulty)
    {
        float raw = seconds / 60f * 2f + kills / 80f + level + (bossKilled ? 60f : 0f);
        return Mathf.Max(1, Mathf.RoundToInt(raw * difficulty.Reward));
    }

    public static void Save()
    {
        EnsureLoaded();
        var cfg = new ConfigFile();
        cfg.SetValue("meta", "fichas", _fichas);
        cfg.SetValue("meta", "earned", _totalEarned);
        foreach (MetaDef def in All) cfg.SetValue("levels", def.Id, Level(def.Id));
        cfg.Save(FilePath);
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        var cfg = new ConfigFile();
        if (cfg.Load(FilePath) != Error.Ok) return;

        _fichas = Math.Max(0, cfg.GetValue("meta", "fichas", 0).AsInt32());
        _totalEarned = Math.Max(0, cfg.GetValue("meta", "earned", 0).AsInt32());
        foreach (MetaDef def in All)
            _levels[def.Id] = Mathf.Clamp(cfg.GetValue("levels", def.Id, 0).AsInt32(), 0, def.MaxLevel);
    }
}
