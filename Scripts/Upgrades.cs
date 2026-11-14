using System.Collections.Generic;

namespace DeadFrequencyHorda;

/// <summary>Um "anúncio" da rádio 96.6: upgrade oferecido ao subir de nível.</summary>
public sealed class UpgradeDef
{
    public string Id;
    public string Name;
    /// <summary>Texto mostrado ao oferecer cada nível (índice 0 = nível 1).</summary>
    public string[] Levels;
    /// <summary>Nível com que a van já começa a partida.</summary>
    public int StartLevel;

    public int MaxLevel => Levels.Length;
}

public static class UpgradeCatalog
{
    public const string Antena = "antena";
    public const string Som = "som";
    public const string Parachoque = "parachoque";
    public const string Motor = "motor";
    public const string Lataria = "lataria";
    public const string Ima = "ima";

    public static readonly UpgradeDef[] All =
    {
        new UpgradeDef
        {
            Id = Antena, Name = "Antena 96.6", StartLevel = 1,
            Levels = new[]
            {
                "Dispara ondas de rádio no inimigo mais próximo.",
                "+25% de dano e dispara mais rápido.",
                "Dispara 2 ondas por vez.",
                "As ondas atravessam 1 inimigo.",
                "Dispara 3 ondas por vez.",
                "+50% de dano e dispara mais rápido.",
            },
        },
        new UpgradeDef
        {
            Id = Som, Name = "Carro de Som",
            Levels = new[]
            {
                "Pulso sonoro em volta da van a cada 4 s.",
                "Pulso maior e mais forte.",
                "Pulso maior e mais frequente.",
                "Pulso maior e mais forte.",
                "Pulso enorme, empurra a horda longe.",
            },
        },
        new UpgradeDef
        {
            Id = Parachoque, Name = "Para-choque de Trilho",
            Levels = new[]
            {
                "+40% de dano ao atropelar.",
                "+40% de dano e perde menos velocidade.",
                "+40% de dano e perde menos velocidade.",
                "+40% de dano e perde menos velocidade.",
                "Atropelar quase não freia a van.",
            },
        },
        new UpgradeDef
        {
            Id = Motor, Name = "Motor Envenenado",
            Levels = new[]
            {
                "+10% de velocidade e aceleração.",
                "+10% de velocidade e aceleração.",
                "+10% de velocidade e aceleração.",
                "+10% de velocidade e aceleração.",
                "+10% de velocidade e aceleração.",
            },
        },
        new UpgradeDef
        {
            Id = Lataria, Name = "Lataria Reforçada",
            Levels = new[]
            {
                "+20 de vida máxima e conserta 30.",
                "+20 de vida máxima e conserta 30.",
                "+20 de vida máxima e conserta 30.",
                "+20 de vida máxima e conserta 30.",
                "+20 de vida máxima e conserta 30.",
            },
        },
        new UpgradeDef
        {
            Id = Ima, Name = "Ímã de Geladeira",
            Levels = new[]
            {
                "+50% de alcance para puxar sinais.",
                "+50% de alcance para puxar sinais.",
                "+50% de alcance para puxar sinais.",
                "+50% de alcance para puxar sinais.",
            },
        },
    };

    public static Dictionary<string, int> StartingLevels()
    {
        var levels = new Dictionary<string, int>();
        foreach (UpgradeDef def in All)
            levels[def.Id] = def.StartLevel;
        return levels;
    }
}
