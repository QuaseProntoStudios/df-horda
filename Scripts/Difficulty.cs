using System;

namespace DeadFrequencyHorda;

public sealed class DifficultyDef
{
    public string Name;
    public string Description;
    /// <summary>Multiplicadores sobre vida, dano, velocidade e quantidade de inimigos.</summary>
    public float EnemyHp = 1f;
    public float EnemyDamage = 1f;
    public float EnemySpeed = 1f;
    public float SpawnRate = 1f;
    /// <summary>Chance de cair kit de reparo (1 = padrão).</summary>
    public float RepairDrop = 1f;
    public float XpGain = 1f;
}

public static class Difficulties
{
    public static readonly DifficultyDef[] All =
    {
        new DifficultyDef
        {
            Name = "FÁCIL",
            Description = "Para aprender a dirigir: inimigos fracos e mais reparos.",
            EnemyHp = 0.75f, EnemyDamage = 0.6f, EnemySpeed = 0.93f, SpawnRate = 0.75f, RepairDrop = 2f, XpGain = 1.15f,
        },
        new DifficultyDef
        {
            Name = "NORMAL",
            Description = "O turno padrão. Parar a van custa caro.",
        },
        new DifficultyDef
        {
            Name = "DIFÍCIL",
            Description = "Horda maior, mais forte e menos reparos.",
            EnemyHp = 1.4f, EnemyDamage = 1.4f, EnemySpeed = 1.06f, SpawnRate = 1.3f, RepairDrop = 0.7f,
        },
        new DifficultyDef
        {
            Name = "PESADELO",
            Description = "Quase sem folga. Só para quem domina a van.",
            EnemyHp = 1.9f, EnemyDamage = 2f, EnemySpeed = 1.12f, SpawnRate = 1.7f, RepairDrop = 0.4f,
        },
    };

    public static DifficultyDef Get(int index)
    {
        return All[Math.Clamp(index, 0, All.Length - 1)];
    }
}
