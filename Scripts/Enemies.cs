using Godot;

namespace DeadFrequencyHorda;

/// <summary>Um tipo de inimigo. Os números são base: o Game aplica tempo e dificuldade.</summary>
public sealed class EnemyDef
{
    public string Id;
    public string Name;
    /// <summary>Arte em Art/, com 2 quadros lado a lado.</summary>
    public string Texture;
    public float Hp;
    public float Speed;
    /// <summary>Dano por contato (ou do cuspe, ou da explosão).</summary>
    public float Damage;
    public float Radius;
    /// <summary>Valor do sinal (XP) que larga ao morrer.</summary>
    public float Xp;
    public float FirstSeen;
    public float Weight;
    public bool Ranged;
    public bool Explosive;
    /// <summary>Atropelar segura a van em vez de só empurrar o inimigo.</summary>
    public bool Heavy;
    public bool Elite;
    public bool Boss;
    public float PreferredRange;
    public float ShotInterval;
    public Color Tint = Colors.White;
}

public static class EnemyCatalog
{
    public static readonly EnemyDef Zombie = new EnemyDef
    {
        Id = "zumbi", Name = "Zumbi", Texture = "zombie",
        Hp = 6f, Speed = 27f, Damage = 6f, Radius = 5f, Xp = 1f, FirstSeen = 0f, Weight = 10f,
    };

    public static readonly EnemyDef Runner = new EnemyDef
    {
        Id = "corredor", Name = "Corredor", Texture = "runner",
        Hp = 3.5f, Speed = 58f, Damage = 4f, Radius = 4.5f, Xp = 1f, FirstSeen = 60f, Weight = 3.5f,
    };

    public static readonly EnemyDef Spitter = new EnemyDef
    {
        Id = "cuspidor", Name = "Cuspidor", Texture = "spitter",
        Hp = 9f, Speed = 24f, Damage = 7f, Radius = 5f, Xp = 2f, FirstSeen = 140f, Weight = 1.6f,
        Ranged = true, PreferredRange = 70f, ShotInterval = 2.3f,
    };

    public static readonly EnemyDef Brute = new EnemyDef
    {
        Id = "brutamontes", Name = "Brutamontes", Texture = "brute",
        Hp = 45f, Speed = 17f, Damage = 13f, Radius = 9f, Xp = 6f, FirstSeen = 210f, Weight = 1.2f,
        Heavy = true,
    };

    public static readonly EnemyDef Bomber = new EnemyDef
    {
        Id = "bomba", Name = "Bomba Ambulante", Texture = "bomber",
        Hp = 7f, Speed = 40f, Damage = 22f, Radius = 5f, Xp = 2f, FirstSeen = 330f, Weight = 1.4f,
        Explosive = true,
    };

    public static readonly EnemyDef Alpha = new EnemyDef
    {
        Id = "alfa", Name = "Brutamontes Alfa", Texture = "brute",
        Hp = 420f, Speed = 21f, Damage = 20f, Radius = 10f, Xp = 40f,
        Heavy = true, Elite = true, Tint = new Color(1f, 0.78f, 0.35f),
    };

    public static readonly EnemyDef Boss = new EnemyDef
    {
        Id = "chefao", Name = "ZUMBÃO DA 96.6", Texture = "boss",
        Hp = 5000f, Speed = 22f, Damage = 28f, Radius = 14f, Xp = 200f,
        Heavy = true, Boss = true,
    };

    /// <summary>Tipos sorteados nas levas normais (elite e chefe têm hora marcada).</summary>
    public static readonly EnemyDef[] Spawnable = { Zombie, Runner, Spitter, Brute, Bomber };
}
