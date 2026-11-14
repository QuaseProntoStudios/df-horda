using System;
using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

public enum Rarity { Comum, Incomum, Raro, Epico, Lendario }

public static class Rarities
{
    public static readonly string[] Names = { "COMUM", "INCOMUM", "RARO", "ÉPICO", "LENDÁRIO" };

    /// <summary>Quanto cada raridade multiplica o efeito do upgrade.</summary>
    public static readonly float[] Mult = { 1f, 1.25f, 1.6f, 2.2f, 3.2f };

    public static readonly Color[] Tint =
    {
        new Color(0.80f, 0.82f, 0.80f), // comum: cinza
        new Color(0.35f, 0.90f, 0.40f), // incomum: verde
        new Color(0.30f, 0.60f, 1.00f), // raro: azul
        new Color(0.75f, 0.40f, 1.00f), // épico: roxo
        new Color(1.00f, 0.72f, 0.15f), // lendário: dourado
    };

    private static readonly float[] BaseWeight = { 62f, 25f, 9.5f, 3f, 0.5f };

    /// <summary>luck 1 = chances normais; cada ponto acima puxa as raridades altas para cima.</summary>
    public static Rarity Roll(Random rng, float luck)
    {
        var weights = new float[BaseWeight.Length];
        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = BaseWeight[i] * Mathf.Pow(Mathf.Max(luck, 1f), i);
            total += weights[i];
        }

        float roll = (float)rng.NextDouble() * total;
        for (int i = 0; i < weights.Length; i++)
        {
            if (roll < weights[i]) return (Rarity)i;
            roll -= weights[i];
        }
        return Rarity.Comum;
    }
}

public enum UpKind { Weapon, Passive, Utility }

/// <summary>Um "anúncio" da rádio 96.6: upgrade oferecido ao subir de nível.</summary>
public sealed class UpgradeDef
{
    public string Id;
    public string Name;
    public UpKind Kind;
    /// <summary>Quantas vezes pode ser escolhido (int.MaxValue = sem limite).</summary>
    public int MaxLevel;
    public int StartLevel;
    /// <summary>Armas: texto do que muda em cada nível (índice 0 = nível 1).</summary>
    public string[] LevelText;
    /// <summary>Passivos e utilitários: texto com {0} no lugar do valor.</summary>
    public string Text;
    /// <summary>Valor base de cada escolha comum (0.1 = 10% quando Percent).</summary>
    public float PerPick;
    public bool Percent;
    /// <summary>Só aparece quando faltam opções de verdade.</summary>
    public bool Fallback;
}

/// <summary>Uma oferta da tela de nível: qual upgrade e em qual raridade.</summary>
public sealed class Offer
{
    public UpgradeDef Def;
    public Rarity Rarity;
}

public static class UpgradeCatalog
{
    // Armas
    public const string Antena = "antena";
    public const string Som = "som";
    public const string Chinelada = "chinelada";
    public const string Ovos = "ovos";
    public const string Escapamento = "escapamento";

    // Passivos
    public const string Parachoque = "parachoque";
    public const string Motor = "motor";
    public const string Lataria = "lataria";
    public const string Ima = "ima";
    public const string Amortecedor = "amortecedor";
    public const string Alternador = "alternador";
    public const string Calibragem = "calibragem";
    public const string Velas = "velas";
    public const string Radio = "radio";
    public const string Trevo = "trevo";

    // Sem fim: entram quando tudo o resto acabou
    public const string Sucata = "sucata";
    public const string Reparo = "reparo";
    public const string SinalForte = "sinalforte";

    /// <summary>Quanto cada raridade-ponto de uma arma aumenta o dano dela.</summary>
    public const float WeaponPowerStep = 0.22f;

    public static readonly UpgradeDef[] All =
    {
        new UpgradeDef
        {
            Id = Antena, Name = "Antena 96.6", Kind = UpKind.Weapon, StartLevel = 1,
            LevelText = new[]
            {
                "Dispara ondas de rádio no inimigo mais próximo.",
                "Dispara mais rápido.",
                "Dispara 2 ondas por vez.",
                "As ondas atravessam 2 inimigos.",
                "Dispara 3 ondas por vez.",
                "Dispara 4 ondas, bem mais rápido.",
            },
        },
        new UpgradeDef
        {
            Id = Som, Name = "Carro de Som", Kind = UpKind.Weapon,
            LevelText = new[]
            {
                "Pulso sonoro em volta da van a cada 4 s.",
                "Pulso maior e mais forte.",
                "Pulso maior e mais frequente.",
                "Pulso maior e mais forte.",
                "Pulso enorme que empurra a horda longe.",
            },
        },
        new UpgradeDef
        {
            Id = Chinelada, Name = "Chinelada", Kind = UpKind.Weapon,
            LevelText = new[]
            {
                "Arremessa um chinelo giratório que fura a horda.",
                "Arremessa mais rápido e bate mais forte.",
                "Arremessa 2 chinelos em leque.",
                "Chinelos maiores e mais velozes.",
                "Arremessa 3 chinelos em leque.",
                "Arremessa 5 chinelos em leque aberto.",
            },
        },
        new UpgradeDef
        {
            Id = Ovos, Name = "Carro do Ovo", Kind = UpKind.Weapon,
            LevelText = new[]
            {
                "2 ovos giram em volta da van batendo nos inimigos.",
                "+1 ovo e gira mais rápido.",
                "+1 ovo e órbita maior.",
                "+1 ovo e mais dano.",
                "+1 ovo e gira muito mais rápido.",
                "+3 ovos: uma chuva em órbita.",
            },
        },
        new UpgradeDef
        {
            Id = Escapamento, Name = "Escapamento Tóxico", Kind = UpKind.Weapon,
            LevelText = new[]
            {
                "A van deixa nuvens de gás que machucam quem passar.",
                "As nuvens duram mais.",
                "As nuvens ficam maiores.",
                "Mais nuvens e mais dano.",
                "O gás deixa os inimigos lentos.",
                "Nuvens enormes e muito mais dano.",
            },
        },

        Passive(Parachoque, "Para-choque de Trilho", 0.35f, true, 6, "+{0}% de dano ao atropelar"),
        Passive(Motor, "Motor Envenenado", 0.09f, true, 6, "+{0}% de velocidade e aceleração"),
        Passive(Lataria, "Lataria Reforçada", 20f, false, 6, "+{0} de vida máxima e conserta o mesmo tanto"),
        Passive(Ima, "Ímã de Geladeira", 0.45f, true, 5, "+{0}% de alcance do ímã de sinais"),
        Passive(Amortecedor, "Amortecedor", 0.10f, true, 5, "+{0}% de resistência a dano"),
        Passive(Alternador, "Alternador Novo", 0.4f, false, 5, "Conserta {0} de vida por segundo"),
        Passive(Calibragem, "Calibragem de Precisão", 0.10f, true, 6, "+{0}% de dano de todas as armas"),
        Passive(Velas, "Velas Novas", 0.08f, true, 5, "+{0}% de cadência de todas as armas"),
        Passive(Radio, "Rádio Pirata", 0.10f, true, 5, "+{0}% de sinais (XP) ganhos"),
        Passive(Trevo, "Trevo no Retrovisor", 0.12f, true, 5, "+{0}% de chance de raridade melhor"),

        new UpgradeDef
        {
            Id = Sucata, Name = "Sucata Aproveitada", Kind = UpKind.Passive, Fallback = true,
            MaxLevel = int.MaxValue, PerPick = 0.04f, Percent = true,
            Text = "+{0}% de dano de todas as armas",
        },
        new UpgradeDef
        {
            Id = Reparo, Name = "Reparo Completo", Kind = UpKind.Utility, Fallback = true,
            MaxLevel = int.MaxValue, PerPick = 0.5f, Percent = true,
            Text = "Conserta {0}% da vida da van agora",
        },
        new UpgradeDef
        {
            Id = SinalForte, Name = "Sinal Forte", Kind = UpKind.Utility, Fallback = true,
            MaxLevel = int.MaxValue, PerPick = 0.35f, Percent = true,
            Text = "Ganha {0}% do XP do próximo nível agora",
        },
    };

    private static UpgradeDef Passive(string id, string name, float perPick, bool percent, int max, string text)
    {
        return new UpgradeDef
        {
            Id = id, Name = name, Kind = UpKind.Passive,
            MaxLevel = max, PerPick = perPick, Percent = percent, Text = text,
        };
    }

    static UpgradeCatalog()
    {
        // Armas: o MaxLevel é o tamanho da lista de textos.
        foreach (UpgradeDef def in All)
        {
            if (def.Kind == UpKind.Weapon) def.MaxLevel = def.LevelText.Length;
        }
    }

    public static UpgradeDef Find(string id)
    {
        foreach (UpgradeDef def in All)
        {
            if (def.Id == id) return def;
        }
        return null;
    }

    /// <summary>Nível e poder iniciais de cada upgrade (a Antena já vem equipada).</summary>
    public static void FillStart(Dictionary<string, int> levels, Dictionary<string, float> power)
    {
        foreach (UpgradeDef def in All)
        {
            levels[def.Id] = def.StartLevel;
            power[def.Id] = def.StartLevel;
        }
    }

    /// <summary>Texto mostrado na tela de nível para esta oferta.</summary>
    public static string Describe(UpgradeDef def, Rarity rarity, int currentLevel)
    {
        float mult = Rarities.Mult[(int)rarity];
        if (def.Kind == UpKind.Weapon)
        {
            string text = def.LevelText[Math.Min(currentLevel, def.LevelText.Length - 1)];
            return $"{text}\n+{WeaponPowerStep * 100f * mult:0}% de dano desta arma";
        }

        float amount = def.PerPick * mult;
        string shown;
        if (def.Kind == UpKind.Utility) shown = $"{Math.Min(100f, amount * 100f):0}";
        else if (def.Percent) shown = $"{amount * 100f:0}";
        else shown = $"{amount:0.#}";
        return def.Text.Replace("{0}", shown);
    }
}
