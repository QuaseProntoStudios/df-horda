using System;
using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

public sealed class AchievementDef
{
    public string Id;
    public string Name;
    public string Description;
    /// <summary>Fichas que a conquista paga ao ser desbloqueada.</summary>
    public int Reward;
    /// <summary>Escondida: a descrição só aparece depois de desbloquear.</summary>
    public bool Secret;
}

/// <summary>
/// Conquistas salvas em user://achievements.cfg, com estatísticas de todas as partidas.
/// Quem desbloqueia chama Unlock(id); a tela de aviso escuta o evento Unlocked.
/// </summary>
public static class Achievements
{
    private const string FilePath = "user://achievements.cfg";

    public static readonly AchievementDef[] All =
    {
        A("primeira_volta", "Primeira Volta", "Termine uma partida (vencendo ou não).", 5),
        A("atropelo", "Primeiro Atropelo", "Derrote seu primeiro inimigo.", 3),
        A("centena", "Centenária", "Derrote 100 inimigos em uma partida.", 5),
        A("quinhentos", "Horda no Retrovisor", "Derrote 500 inimigos em uma partida.", 10),
        A("mil", "Mil Zumbis", "Derrote 1.000 inimigos em uma partida.", 15),
        A("tres_mil", "Ceifador de Asfalto", "Derrote 3.000 inimigos em uma partida.", 30),
        A("dedetizadora", "Dedetizadora", "Derrote 10.000 inimigos somando todas as partidas.", 25),
        A("atropelador", "Rolo Compressor", "Atropele 150 inimigos em uma partida.", 10),
        A("nivel10", "Subindo a Serra", "Chegue ao nível 10.", 5),
        A("nivel25", "Quase no Topo", "Chegue ao nível 25.", 10),
        A("nivel40", "Sinal Forte", "Chegue ao nível 40.", 20),
        A("tempo5", "Madrugada", "Sobreviva 5 minutos.", 5),
        A("tempo10", "Turno Dobrado", "Sobreviva 10 minutos.", 10),
        A("intocavel", "Intocável", "Fique 90 segundos sem tomar dano.", 15),
        A("alfa", "Caçador de Alfa", "Derrote um Brutamontes Alfa.", 10),
        A("bombas", "Desarmador", "Derrote 10 Bombas Ambulantes antes que explodam.", 10),
        A("chefe", "Zumbão Aposentado", "Derrote o Zumbão da 96.6.", 40),
        A("chefe_dificil", "Sinal Limpo", "Derrote o chefe na dificuldade Difícil.", 60),
        A("chefe_pesadelo", "Lenda da 96.6", "Derrote o chefe no Pesadelo.", 120, true),
        A("pesadelo5", "Pesadelo Acordado", "Sobreviva 5 minutos no Pesadelo.", 40),
        A("epico", "Sorte Grande", "Escolha um upgrade épico.", 5),
        A("lendario", "Lendário!", "Escolha um upgrade lendário.", 15),
        A("arsenal", "Arsenal Completo", "Tenha as 5 armas na mesma partida.", 15),
        A("arma_maxima", "Arma no Talo", "Leve uma arma ao nível máximo.", 15),
        A("reparo", "Primeiros Socorros", "Pegue um kit de reparo.", 3),
        A("retomada", "Volta e Meia", "Retome uma partida salva.", 3),
        A("oficina", "Oficina Aberta", "Compre seu primeiro upgrade permanente.", 5),
        A("cliente_fiel", "Cliente Fiel", "Compre 10 níveis de upgrades permanentes.", 15),
        A("mecanico", "Mecânico-Chefe", "Leve um upgrade permanente ao nível máximo.", 25),
        A("segunda_chance", "Ainda Não Foi Dessa", "Use a Segunda Chance da oficina.", 10, true),
    };

    private static readonly HashSet<string> _unlocked = new();
    private static readonly Dictionary<string, double> _stats = new();
    private static bool _loaded;

    /// <summary>Disparado quando uma conquista nova é desbloqueada.</summary>
    public static event Action<AchievementDef> Unlocked;

    private static AchievementDef A(string id, string name, string description, int reward, bool secret = false)
    {
        return new AchievementDef { Id = id, Name = name, Description = description, Reward = reward, Secret = secret };
    }

    public static int UnlockedCount
    {
        get { EnsureLoaded(); return _unlocked.Count; }
    }

    public static bool IsUnlocked(string id)
    {
        EnsureLoaded();
        return _unlocked.Contains(id);
    }

    public static AchievementDef Find(string id)
    {
        foreach (AchievementDef def in All)
        {
            if (def.Id == id) return def;
        }
        return null;
    }

    public static void Unlock(string id)
    {
        EnsureLoaded();
        if (_unlocked.Contains(id)) return;

        AchievementDef def = Find(id);
        if (def == null) return;

        _unlocked.Add(id);
        Save();
        if (def.Reward > 0) MetaProgress.AddFichas(def.Reward, false);
        Unlocked?.Invoke(def);
    }

    public static double Stat(string key)
    {
        EnsureLoaded();
        return _stats.TryGetValue(key, out double value) ? value : 0.0;
    }

    public static void AddStat(string key, double amount)
    {
        EnsureLoaded();
        _stats[key] = Stat(key) + amount;
    }

    public static void Save()
    {
        EnsureLoaded();
        var cfg = new ConfigFile();
        cfg.SetValue("unlocked", "ids", string.Join(",", _unlocked));
        foreach (KeyValuePair<string, double> stat in _stats) cfg.SetValue("stats", stat.Key, stat.Value);
        cfg.Save(FilePath);
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        var cfg = new ConfigFile();
        if (cfg.Load(FilePath) != Error.Ok) return;

        string ids = cfg.GetValue("unlocked", "ids", "").AsString();
        foreach (string id in ids.Split(',', StringSplitOptions.RemoveEmptyEntries)) _unlocked.Add(id);
        if (!cfg.HasSection("stats")) return;
        foreach (string key in cfg.GetSectionKeys("stats")) _stats[key] = cfg.GetValue("stats", key, 0.0).AsDouble();
    }
}
