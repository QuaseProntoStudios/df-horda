using System;
using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>
/// A partida inteira: horda, armas, sinais (XP), upgrades com raridade, chefe e fim de jogo.
/// Inimigos, projéteis e sinais são objetos simples com um Sprite2D cada, atualizados
/// aqui por distância (sem física do Godot), o que aguenta centenas na tela.
/// </summary>
public partial class Game : Node2D
{
    private enum State { Playing, ChoosingUpgrade, Paused, GameOver, Victory }

    private sealed class Enemy
    {
        public EnemyDef Def;
        public Sprite2D Sprite;
        public Vector2 Position;
        public Vector2 Knockback;
        public float Hp;
        public float MaxHp;
        public float Speed;
        public float RamCooldown;
        public float EggCooldown;
        public float GasCooldown;
        public float SlowTime;
        public float FlashTime;
        public float AnimTime;
        public float ShotTimer;
        public float Fuse = -1f;
        public float SummonTimer = 6f;
        public bool Dead;
    }

    private sealed class Shot
    {
        public Sprite2D Sprite;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Damage;
        public float Life;
        public float Spin;
        public float HitRadius;
        public int Pierce;
        public bool Hostile;
        public bool Dead;
        public readonly List<Enemy> AlreadyHit = new();
    }

    private sealed class Gem
    {
        public Sprite2D Sprite;
        public Vector2 Position;
        public float Value;
        public bool Attracted;
        public bool Dead;
    }

    private sealed class Cloud
    {
        public Sprite2D Sprite;
        public Vector2 Position;
        public float Life;
        public float MaxLife;
        public float Radius;
        public float Damage;
        public bool Slows;
    }

    private sealed class Pickup
    {
        public Sprite2D Sprite;
        public Vector2 Position;
        public bool Dead;
    }

    // ---- Regras da partida ----
    private const float BossTime = 15f * 60f;
    private const int MaxEnemies = 420;
    private const int MaxGems = 500;
    private const int MaxClouds = 70;
    private const int MaxPickups = 6;
    private const float SpawnRadius = 205f;
    private const float LeashRadius = 330f;
    private const float VanInvulnerability = 0.5f;
    private const float ContactStack = 0.3f;
    private const int ContactCap = 10;
    private const float RamMinSpeed = 45f;
    private const float RamBaseDamage = 10f;
    private const float BulletSpeed = 190f;
    private const float BulletRange = 150f;
    private const float BaseMagnet = 22f;
    private const float GroundTile = 64f;
    private const float RepairHeal = 0.25f;

    private static readonly float[] HordeTimes = { 150f, 330f, 510f, 690f };
    private static readonly float[] EliteTimes = { 280f, 420f, 560f, 700f, 840f };

    private State _state = State.Playing;
    private readonly Random _random = new();

    private Van _van;
    private Sprite2D _ground;
    private Camera2D _camera;
    private Node2D _world;
    private Hud _hud;
    private UpgradeScreen _upgradeScreen;
    private GameAudio _audio;
    private MenuPanel _pause;

    private readonly List<Enemy> _enemies = new();
    private readonly List<Shot> _shots = new();
    private readonly List<Gem> _gems = new();
    private readonly List<Cloud> _clouds = new();
    private readonly List<Pickup> _pickups = new();
    private readonly List<Sprite2D> _eggs = new();
    private readonly Dictionary<EnemyDef, int> _typeCount = new();
    private Dictionary<string, int> _levels = new();
    private Dictionary<string, float> _power = new();

    private DifficultyDef _diff;
    private int _diffIndex;
    private Enemy _boss;
    private bool _bossSpawned;
    private bool _bossDead;

    private float _time;
    private float _spawnTimer;
    private float _antennaTimer;
    private float _pulseTimer;
    private float _chineloTimer;
    private float _gasTimer;
    private float _eggAngle;
    private float _invulnerable;
    private float _ramSoundCooldown;
    private float _hurtSoundCooldown;
    private float _shake;
    private float _hp = 100f;
    private float _maxHp = 100f;
    private float _xp;
    private int _level = 1;
    private int _kills;
    private int _hordeIndex;
    private int _eliteIndex;

    // Derivados dos upgrades (ApplyStats).
    private float _dmgMult = 1f;
    private float _rate = 1f;
    private float _ramMult = 1f;
    private float _ramResist;
    private float _magnet = BaseMagnet;
    private float _takenMult = 1f;
    private float _regen;
    private float _xpMult = 1f;
    private float _luck = 1f;

    // Escalas por tempo e dificuldade (UpdateScales).
    private float _hpScale = 1f;
    private float _dmgScale = 1f;
    private float _speedScale = 1f;

    public override void _Ready()
    {
        GameSettings.Init();

        RunData loaded = null;
        if (Session.ContinueRun)
        {
            loaded = RunSave.Load();
            Session.ContinueRun = false;
        }

        _diffIndex = loaded?.Difficulty ?? Session.Difficulty;
        _diff = Difficulties.Get(_diffIndex);
        Session.Difficulty = _diffIndex;

        UpgradeCatalog.FillStart(_levels, _power);

        _world = new Node2D();
        AddChild(_world);

        _ground = new Sprite2D
        {
            Texture = Assets.Texture("ground"),
            TextureRepeat = TextureRepeatEnum.Enabled,
            RegionEnabled = true,
            RegionRect = new Rect2(0f, 0f, GroundTile * 8f, GroundTile * 6f),
            ZIndex = -10,
        };
        _world.AddChild(_ground);

        _van = new Van();
        _world.AddChild(_van);

        _camera = new Camera2D();
        _van.AddChild(_camera);
        _camera.MakeCurrent();

        _audio = new GameAudio();
        AddChild(_audio);

        _hud = new Hud();
        AddChild(_hud);

        _upgradeScreen = new UpgradeScreen();
        AddChild(_upgradeScreen);
        _upgradeScreen.Chosen += OnUpgradeChosen;

        BuildPauseMenu();

        if (loaded != null) Restore(loaded);

        ApplyStats();
        if (loaded == null) _hp = _maxHp;
        UpdateScales();
        RefreshHud();
        UpdateGround();
        _hud.Toast(loaded != null ? "PARTIDA RETOMADA" : "DIRIJA! PARADO, A HORDA TE COME", UiStyle.Green, 3.5f);
    }

    private void Restore(RunData data)
    {
        foreach (UpgradeDef def in UpgradeCatalog.All)
        {
            _levels[def.Id] = data.Levels[def.Id];
            _power[def.Id] = data.Power[def.Id];
        }

        _level = data.Level;
        _kills = data.Kills;
        _hordeIndex = data.HordeIndex;
        _eliteIndex = data.EliteIndex;
        _time = data.Time;
        _xp = data.Xp;
        _van.Position = new Vector2(data.VanX, data.VanY);
        _van.Heading = data.Heading;

        ApplyStats();
        _hp = Mathf.Clamp(data.Hp, 1f, _maxHp);
        if (_levels[UpgradeCatalog.Som] > 0) _pulseTimer = 1f;
    }

    // ---------------------------------------------------------------- Laço principal

    public override void _Process(double delta)
    {
        if (_state != State.Playing) return;
        float dt = Mathf.Min((float)delta, 0.05f);

        _time += dt;
        UpdateScales();
        UpdateTimeline();

        _van.Tick(dt);
        _audio.SetEngine(_van.Speed / _van.MaxSpeed);
        UpdateGround();
        UpdateShake(dt);

        if (_invulnerable > 0f) _invulnerable -= dt;
        if (_ramSoundCooldown > 0f) _ramSoundCooldown -= dt;
        if (_hurtSoundCooldown > 0f) _hurtSoundCooldown -= dt;
        if (_regen > 0f && _hp < _maxHp) _hp = Mathf.Min(_hp + _regen * dt, _maxHp);

        SpawnEnemies(dt);
        UpdateEnemies(dt);
        if (_state != State.Playing) return;

        UpdateAntenna(dt);
        UpdatePulse(dt);
        UpdateChinelada(dt);
        UpdateEggs(dt);
        UpdateGas(dt);
        UpdateShots(dt);
        UpdateGems(dt);
        UpdatePickups();
        RemoveDead();
        RefreshHud();

        if (_state == State.Playing && _xp >= XpToNextLevel())
            LevelUp();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;

        bool pauseKey = key.PhysicalKeycode == Key.P;
        bool escape = key.Keycode == Key.Escape;

        if (_state == State.Playing && (pauseKey || escape))
        {
            OpenPause();
        }
        else if (_state == State.Paused && pauseKey)
        {
            ClosePause();
        }
        else if (_state == State.GameOver || _state == State.Victory)
        {
            if (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter || key.Keycode == Key.Space)
            {
                Session.ContinueRun = false;
                GetTree().ReloadCurrentScene();
            }
            else if (escape)
            {
                GetTree().ChangeSceneToFile(Session.MenuScene);
            }
        }
    }

    // ---------------------------------------------------------------- Escalas e linha do tempo

    private void UpdateScales()
    {
        float t = _time;
        float ramp = t / 450f;
        _hpScale = (1f + t / 200f + ramp * ramp) * _diff.EnemyHp;
        _dmgScale = _diff.EnemyDamage * (1f + t / 600f);
        _speedScale = _diff.EnemySpeed * (1f + t / 2400f);
    }

    private void UpdateTimeline()
    {
        if (_hordeIndex < HordeTimes.Length && _time >= HordeTimes[_hordeIndex])
        {
            StartHorde(_hordeIndex);
            _hordeIndex++;
        }

        if (_eliteIndex < EliteTimes.Length && _time >= EliteTimes[_eliteIndex])
        {
            SpawnElite();
            _eliteIndex++;
        }

        if (!_bossSpawned && _time >= BossTime)
            SpawnBoss();
    }

    private void StartHorde(int index)
    {
        _hud.Toast("HORDA CHEGANDO!", UiStyle.Red, 3f);
        _audio.PlayEnemy("SFX/bus_horn.wav", -8f, 0.7f);

        int count = Mathf.RoundToInt((34f + 12f * index) * _diff.SpawnRate);
        for (int i = 0; i < count && _enemies.Count < MaxEnemies + 80; i++)
        {
            float angle = Mathf.Tau * i / count + (float)_random.NextDouble() * 0.2f;
            Vector2 position = _van.Position + Vector2.Right.Rotated(angle) * (SpawnRadius + (float)_random.NextDouble() * 30f);
            EnemyDef def = _time >= EnemyCatalog.Runner.FirstSeen && _random.NextDouble() < 0.3 ? EnemyCatalog.Runner : EnemyCatalog.Zombie;
            SpawnEnemy(def, position);
        }
    }

    private void SpawnElite()
    {
        _hud.Toast("BRUTAMONTES ALFA!", new Color(1f, 0.78f, 0.35f), 3f);
        _audio.PlayEnemy("SFX/bus_horn.wav", -8f, 0.85f);
        SpawnEnemy(EnemyCatalog.Alpha, RingPosition());
    }

    private void SpawnBoss()
    {
        _bossSpawned = true;
        _boss = SpawnEnemy(EnemyCatalog.Boss, RingPosition());
        _hud.Toast("ZUMBÃO DA 96.6 CHEGOU!", UiStyle.Red, 4f);
        _audio.PlayEnemy("SFX/bus_horn.wav", -4f, 0.55f);
        _shake = Mathf.Max(_shake, 4f);
    }

    // ---------------------------------------------------------------- Horda

    private void SpawnEnemies(float dt)
    {
        float progress = Mathf.Clamp(_time / BossTime, 0f, 1f);
        _spawnTimer -= dt;
        if (_spawnTimer > 0f) return;

        float interval = Mathf.Lerp(0.8f, 0.09f, progress) / _diff.SpawnRate;
        if (_bossSpawned && !_bossDead) interval /= 0.6f;
        _spawnTimer = interval;

        int batch = 1 + (int)(_time / 110f);
        for (int i = 0; i < batch && _enemies.Count < MaxEnemies; i++)
            SpawnEnemy(PickType(), RingPosition());
    }

    private EnemyDef PickType()
    {
        float total = 0f;
        var weights = new float[EnemyCatalog.Spawnable.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            EnemyDef def = EnemyCatalog.Spawnable[i];
            if (_time < def.FirstSeen || AtCap(def)) continue;
            // Tipo novo entra devagar, não de uma vez.
            float ramp = Mathf.Min(1f, 0.15f + (_time - def.FirstSeen) / 90f);
            weights[i] = def.Weight * ramp;
            total += weights[i];
        }

        if (total <= 0f) return EnemyCatalog.Zombie;
        float roll = (float)_random.NextDouble() * total;
        for (int i = 0; i < weights.Length; i++)
        {
            if (roll < weights[i]) return EnemyCatalog.Spawnable[i];
            roll -= weights[i];
        }
        return EnemyCatalog.Zombie;
    }

    private bool AtCap(EnemyDef def)
    {
        int cap = def == EnemyCatalog.Brute ? 10 : def == EnemyCatalog.Spitter ? 22 : def == EnemyCatalog.Bomber ? 16 : int.MaxValue;
        return _typeCount.TryGetValue(def, out int count) && count >= cap;
    }

    private Enemy SpawnEnemy(EnemyDef def, Vector2 position)
    {
        float hp;
        if (def.Boss) hp = def.Hp * _diff.EnemyHp;
        else if (def.Elite) hp = def.Hp * (1f + _time / 400f) * _diff.EnemyHp;
        else hp = def.Hp * _hpScale;

        var enemy = new Enemy
        {
            Def = def,
            Position = position,
            Hp = hp,
            MaxHp = hp,
            Speed = def.Speed * _speedScale * (0.92f + (float)_random.NextDouble() * 0.16f),
            AnimTime = (float)_random.NextDouble(),
            ShotTimer = def.ShotInterval * (0.4f + (float)_random.NextDouble() * 0.8f),
            Sprite = new Sprite2D { Texture = Assets.Texture(def.Texture), Hframes = 2, ZIndex = def.Boss ? 2 : 1, Modulate = def.Tint },
        };
        if (def.Elite) enemy.Sprite.Scale = new Vector2(1.2f, 1.2f);
        enemy.Sprite.Position = position;
        _world.AddChild(enemy.Sprite);
        _enemies.Add(enemy);
        _typeCount[def] = (_typeCount.TryGetValue(def, out int count) ? count : 0) + 1;
        return enemy;
    }

    /// <summary>Ponto no anel fora da tela, puxado para a frente da van quando ela está correndo.</summary>
    private Vector2 RingPosition()
    {
        float angle = (float)_random.NextDouble() * Mathf.Tau;
        if (_van.Speed > 50f && _random.NextDouble() < 0.5)
            angle = _van.Velocity.Angle() + ((float)_random.NextDouble() - 0.5f) * 1.6f;
        return _van.Position + Vector2.Right.Rotated(angle) * SpawnRadius;
    }

    private void UpdateEnemies(float dt)
    {
        Vector2 vanPosition = _van.Position;
        float vanSpeed = _van.Speed;
        bool ramming = vanSpeed > RamMinSpeed;
        int count = _enemies.Count;

        float contactMax = 0f;
        float contactSum = 0f;
        int contacts = 0;
        float drag = 0f;

        for (int i = 0; i < count; i++)
        {
            Enemy enemy = _enemies[i];
            if (enemy.Dead) continue;
            EnemyDef def = enemy.Def;

            Vector2 toVan = vanPosition - enemy.Position;
            float distance = toVan.Length();

            if (distance > LeashRadius)
            {
                enemy.Position = RingPosition();
                enemy.Knockback = Vector2.Zero;
                toVan = vanPosition - enemy.Position;
                distance = toVan.Length();
            }

            Vector2 direction = distance > 0.01f ? toVan / distance : Vector2.Zero;

            if (enemy.SlowTime > 0f) enemy.SlowTime -= dt;
            if (enemy.RamCooldown > 0f) enemy.RamCooldown -= dt;
            if (enemy.EggCooldown > 0f) enemy.EggCooldown -= dt;
            if (enemy.GasCooldown > 0f) enemy.GasCooldown -= dt;
            if (enemy.FlashTime > 0f) enemy.FlashTime -= dt;

            float speed = enemy.Speed * (enemy.SlowTime > 0f ? 0.55f : 1f);
            if (def.Boss && enemy.Hp < enemy.MaxHp * 0.5f) speed *= 1.3f;

            Vector2 move = direction * speed;
            if (def.Ranged)
            {
                // Cuspidor: segura a distância e cospe.
                if (distance < def.PreferredRange) move = -direction * speed * 0.5f;
                else if (distance < def.PreferredRange + 12f) move = Vector2.Zero;

                enemy.ShotTimer -= dt;
                if (enemy.ShotTimer <= 0f && distance < 150f)
                {
                    enemy.ShotTimer = def.ShotInterval;
                    SpitAt(enemy);
                }
            }

            if (enemy.Fuse >= 0f)
            {
                // Bomba acesa: para e pisca até explodir.
                move = Vector2.Zero;
                enemy.Fuse -= dt;
                if (enemy.Fuse <= 0f)
                {
                    Explode(enemy);
                    continue;
                }
            }
            else if (def.Explosive && distance < 28f)
            {
                enemy.Fuse = 0.6f;
            }

            if (def.Boss)
            {
                enemy.SummonTimer -= dt;
                if (enemy.SummonTimer <= 0f)
                {
                    enemy.SummonTimer = 8f;
                    for (int k = 0; k < 6 && _enemies.Count < MaxEnemies + 80; k++)
                        SpawnEnemy(EnemyCatalog.Zombie, enemy.Position + Vector2.Right.Rotated(k * Mathf.Tau / 6f) * 22f);
                }
            }

            enemy.Position += (move + enemy.Knockback) * dt;
            enemy.Knockback = enemy.Knockback.MoveToward(Vector2.Zero, 320f * dt);

            Separate(enemy, _enemies[(i + 1) % count]);
            Separate(enemy, _enemies[(i + 7) % count]);

            float touch = Van.Radius + def.Radius;
            if (distance < touch && enemy.Fuse < 0f)
            {
                bool rammed = ramming && !def.Boss;
                if (ramming && enemy.RamCooldown <= 0f)
                {
                    enemy.RamCooldown = 0.3f;
                    float damage = RamBaseDamage * _ramMult * (vanSpeed / 100f);
                    enemy.Knockback = _van.Velocity.Normalized() * (def.Heavy ? 50f : 130f) - direction * 50f;
                    DamageEnemy(enemy, damage);
                    _shake = Mathf.Max(_shake, def.Heavy ? 2.5f : 1.2f);
                    if (_ramSoundCooldown <= 0f)
                    {
                        _ramSoundCooldown = 0.08f;
                        _audio.PlayEnemy("SFX/impact_box.wav", -8f, 0.9f + (float)_random.NextDouble() * 0.3f);
                    }
                }

                // Bater em alguém segura a van: os pesados seguram muito mais.
                if (ramming) drag += def.Boss ? 6f : def.Heavy ? 3f : 0.12f;

                // Quem está sendo atropelado não morde, exceto os pesados.
                float factor = !ramming ? 1f : def.Boss ? 1f : def.Heavy ? 0.5f : 0f;
                if (rammed && factor <= 0f) { /* só toma a pancada */ }
                else if (!def.Explosive && contacts < ContactCap)
                {
                    float contactDamage = def.Damage * _dmgScale * factor;
                    contactSum += contactDamage;
                    if (contactDamage > contactMax) contactMax = contactDamage;
                    contacts++;
                }
            }

            enemy.AnimTime += dt;
            enemy.Sprite.Position = enemy.Position;
            enemy.Sprite.Frame = (int)(enemy.AnimTime * 4f * (def.Boss ? 0.6f : 1f)) % 2;
            enemy.Sprite.FlipH = direction.X < 0f;
            enemy.Sprite.Modulate = EnemyColor(enemy);
        }

        if (drag > 0f)
            _van.Velocity *= Mathf.Exp(-drag * (1f - _ramResist) * dt);

        if (contacts > 0 && _invulnerable <= 0f)
        {
            float total = contactMax + ContactStack * (contactSum - contactMax);
            HurtVan(total);
        }
    }

    private static Color EnemyColor(Enemy enemy)
    {
        if (enemy.FlashTime > 0f) return new Color(3f, 3f, 3f);
        if (enemy.Fuse >= 0f)
            return ((int)(enemy.Fuse * 14f) % 2 == 0) ? new Color(3f, 0.6f, 0.4f) : enemy.Def.Tint;
        return enemy.Def.Tint;
    }

    private static void Separate(Enemy a, Enemy b)
    {
        if (ReferenceEquals(a, b) || b.Dead) return;
        Vector2 offset = a.Position - b.Position;
        float distanceSquared = offset.LengthSquared();
        float minimum = a.Def.Radius + b.Def.Radius;
        if (distanceSquared >= minimum * minimum || distanceSquared < 0.0001f) return;

        float distance = Mathf.Sqrt(distanceSquared);
        Vector2 push = offset / distance * ((minimum - distance) * 0.5f);
        a.Position += push;
        b.Position -= push;
    }

    private void SpitAt(Enemy enemy)
    {
        Vector2 direction = (_van.Position - enemy.Position).Normalized();
        var shot = new Shot
        {
            Position = enemy.Position,
            Velocity = direction * 72f,
            Damage = enemy.Def.Damage * _dmgScale,
            Life = 3f,
            HitRadius = Van.Radius + 2f,
            Hostile = true,
            Sprite = new Sprite2D { Texture = Assets.Texture("spit"), ZIndex = 3 },
        };
        shot.Sprite.Position = shot.Position;
        _world.AddChild(shot.Sprite);
        _shots.Add(shot);
        _audio.PlayEnemy("SFX/impact_box.wav", -12f, 1.7f);
    }

    private void Explode(Enemy enemy)
    {
        enemy.Dead = true;
        _kills++;
        Release(enemy);
        SpawnGem(enemy.Position, enemy.Def.Xp);

        const float radius = 36f;
        _world.AddChild(PulseFx.Create(enemy.Position, radius, new Color(1f, 0.45f, 0.15f)));
        _audio.PlayEnemy("SFX/crash_heavy.wav", -6f, 1.1f);
        _shake = Mathf.Max(_shake, 3f);

        if (enemy.Position.DistanceTo(_van.Position) < radius + Van.Radius * 0.5f && _invulnerable <= 0f)
            HurtVan(enemy.Def.Damage * _dmgScale);
    }

    private void DamageEnemy(Enemy enemy, float damage)
    {
        if (enemy.Dead) return;
        enemy.Hp -= damage;
        enemy.FlashTime = 0.08f;
        if (enemy.Hp > 0f) return;

        enemy.Dead = true;
        _kills++;
        Release(enemy);
        SpawnGem(enemy.Position, enemy.Def.Xp);

        if (enemy.Def.Elite) DropRepair(enemy.Position);
        else if (_random.NextDouble() < 0.007 * _diff.RepairDrop) DropRepair(enemy.Position);

        if (enemy.Def.Boss)
        {
            _bossDead = true;
            _shake = 5f;
            EndRun(State.Victory);
        }
    }

    private void Release(Enemy enemy)
    {
        if (_typeCount.TryGetValue(enemy.Def, out int count)) _typeCount[enemy.Def] = Math.Max(0, count - 1);
    }

    private void HurtVan(float damage)
    {
        damage *= _takenMult;
        _hp -= damage;
        _invulnerable = VanInvulnerability;
        _shake = Mathf.Max(_shake, Mathf.Min(2f + damage * 0.08f, 5f));
        _van.Flash();
        _hud.FlashHp();
        if (_hurtSoundCooldown <= 0f)
        {
            _hurtSoundCooldown = 0.25f;
            _audio.PlayEnemy("SFX/crash_light.wav", -5f);
        }

        if (_hp <= 0f)
        {
            _hp = 0f;
            EndRun(State.GameOver);
        }
    }

    // ---------------------------------------------------------------- Armas

    private float WeaponScale(string id)
    {
        return (1f + UpgradeCatalog.WeaponPowerStep * (_power[id] - 1f)) * _dmgMult;
    }

    private Enemy Nearest(Vector2 from, float range, List<Enemy> exclude = null)
    {
        Enemy best = null;
        float bestDistance = range * range;
        foreach (Enemy enemy in _enemies)
        {
            if (enemy.Dead) continue;
            if (exclude != null && exclude.Contains(enemy)) continue;
            float distance = enemy.Position.DistanceSquaredTo(from);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = enemy;
            }
        }
        return best;
    }

    private Shot AddShot(string texture, Vector2 position, Vector2 velocity, float damage, float life, int pierce, float hitRadius)
    {
        var shot = new Shot
        {
            Position = position, Velocity = velocity, Damage = damage, Life = life, Pierce = pierce, HitRadius = hitRadius,
            Sprite = new Sprite2D { Texture = Assets.Texture(texture), ZIndex = 3, Rotation = velocity.Angle() },
        };
        shot.Sprite.Position = position;
        _world.AddChild(shot.Sprite);
        _shots.Add(shot);
        return shot;
    }

    private void UpdateAntenna(float dt)
    {
        int level = _levels[UpgradeCatalog.Antena];
        if (level <= 0) return;

        _antennaTimer -= dt;
        if (_antennaTimer > 0f) return;

        float rate = (1f + 0.2f * (level - 1) + (level >= 6 ? 0.5f : 0f)) * _rate;
        float damage = 6f * WeaponScale(UpgradeCatalog.Antena);
        int shots = level >= 6 ? 4 : level >= 5 ? 3 : level >= 3 ? 2 : 1;
        int pierce = level >= 4 ? 2 : 1;

        var targets = new List<Enemy>(shots);
        for (int shot = 0; shot < shots; shot++)
        {
            Enemy best = Nearest(_van.Position, BulletRange, targets);
            if (best == null) break;
            targets.Add(best);
        }

        if (targets.Count == 0)
        {
            _antennaTimer = 0.1f;
            return;
        }

        _antennaTimer = 0.9f / rate;
        foreach (Enemy target in targets)
        {
            Vector2 direction = (target.Position - _van.Position).Normalized();
            AddShot("wave", _van.Position, direction * BulletSpeed, damage, BulletRange / BulletSpeed * 1.3f, pierce, 8f);
        }
    }

    private void UpdatePulse(float dt)
    {
        int level = _levels[UpgradeCatalog.Som];
        if (level <= 0) return;

        _pulseTimer -= dt;
        if (_pulseTimer > 0f) return;

        _pulseTimer = (level >= 3 ? 3.2f : 4f) / _rate;
        float radius = 40f + 9f * (level - 1);
        float damage = 8f * (1f + 0.3f * (level - 1)) * WeaponScale(UpgradeCatalog.Som);
        float push = level >= 5 ? 260f : 160f;

        Vector2 center = _van.Position;
        foreach (Enemy enemy in _enemies)
        {
            if (enemy.Dead) continue;
            Vector2 offset = enemy.Position - center;
            float distance = offset.Length();
            if (distance > radius) continue;

            float resist = enemy.Def.Boss ? 0.15f : enemy.Def.Heavy ? 0.4f : 1f;
            enemy.Knockback = (distance > 0.01f ? offset / distance : Vector2.Right) * push * resist;
            DamageEnemy(enemy, damage);
        }

        _world.AddChild(PulseFx.Create(center, radius));
        _audio.Play("SFX/bus_horn.wav", -10f, 1.15f);
    }

    private void UpdateChinelada(float dt)
    {
        int level = _levels[UpgradeCatalog.Chinelada];
        if (level <= 0) return;

        _chineloTimer -= dt;
        if (_chineloTimer > 0f) return;

        Enemy target = Nearest(_van.Position, 170f);
        if (target == null)
        {
            _chineloTimer = 0.15f;
            return;
        }

        int[] counts = { 1, 1, 2, 2, 3, 5 };
        int count = counts[Math.Min(level, counts.Length) - 1];
        float interval = 1.6f * (level >= 2 ? 0.8f : 1f) * (level >= 4 ? 0.85f : 1f);
        _chineloTimer = interval / _rate;

        float damage = 9f * (1f + 0.12f * (level - 1)) * WeaponScale(UpgradeCatalog.Chinelada);
        float speed = level >= 4 ? 150f : 120f;
        float radius = level >= 4 ? 9f : 7f;
        float baseAngle = (target.Position - _van.Position).Angle();
        float step = count >= 5 ? 0.3f : 0.24f;

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + (i - (count - 1) * 0.5f) * step;
            Shot shot = AddShot("chinelo", _van.Position, Vector2.Right.Rotated(angle) * speed, damage, 1.3f, 999, radius);
            shot.Spin = 14f;
            if (level >= 4) shot.Sprite.Scale = new Vector2(1.4f, 1.4f);
        }

        _audio.Play(_random.Next(2) == 0 ? "SFX/chinelo_1.wav" : "SFX/chinelo_2.wav", -8f, 0.9f + (float)_random.NextDouble() * 0.25f);
    }

    private void UpdateEggs(float dt)
    {
        int level = _levels[UpgradeCatalog.Ovos];
        int[] counts = { 2, 3, 4, 5, 6, 9 };
        int wanted = level <= 0 ? 0 : counts[Math.Min(level, counts.Length) - 1];

        while (_eggs.Count < wanted)
        {
            var egg = new Sprite2D { Texture = Assets.Texture("egg"), ZIndex = 3 };
            _world.AddChild(egg);
            _eggs.Add(egg);
        }
        while (_eggs.Count > wanted)
        {
            _eggs[^1].QueueFree();
            _eggs.RemoveAt(_eggs.Count - 1);
        }
        if (wanted == 0) return;

        float spin = (level >= 5 ? 4.2f : level >= 2 ? 3.2f : 2.4f) * _rate;
        float radius = level >= 3 ? 32f : 26f;
        _eggAngle += spin * dt;

        float damage = 5f * (1f + 0.15f * (level - 1)) * WeaponScale(UpgradeCatalog.Ovos);
        for (int i = 0; i < _eggs.Count; i++)
        {
            float angle = _eggAngle + Mathf.Tau * i / _eggs.Count;
            Vector2 position = _van.Position + Vector2.Right.Rotated(angle) * radius;
            _eggs[i].Position = position;
            _eggs[i].Rotation = angle * 2f;

            foreach (Enemy enemy in _enemies)
            {
                if (enemy.Dead || enemy.EggCooldown > 0f) continue;
                float reach = enemy.Def.Radius + 4f;
                if (enemy.Position.DistanceSquaredTo(position) > reach * reach) continue;

                enemy.EggCooldown = 0.35f;
                enemy.Knockback += (enemy.Position - _van.Position).Normalized() * (enemy.Def.Heavy ? 20f : 60f);
                DamageEnemy(enemy, damage);
            }
        }
    }

    private void UpdateGas(float dt)
    {
        int level = _levels[UpgradeCatalog.Escapamento];

        // As nuvens já soltas continuam valendo mesmo se algo mudar.
        float damageScale = level > 0 ? WeaponScale(UpgradeCatalog.Escapamento) : 0f;
        for (int i = _clouds.Count - 1; i >= 0; i--)
        {
            Cloud cloud = _clouds[i];
            cloud.Life -= dt;
            if (cloud.Life <= 0f)
            {
                cloud.Sprite.QueueFree();
                _clouds.RemoveAt(i);
                continue;
            }

            float alpha = Mathf.Clamp(cloud.Life / cloud.MaxLife * 2f, 0f, 1f) * 0.75f;
            cloud.Sprite.Modulate = new Color(1f, 1f, 1f, alpha);

            float radiusSquared = cloud.Radius * cloud.Radius;
            foreach (Enemy enemy in _enemies)
            {
                if (enemy.Dead) continue;
                if (enemy.Position.DistanceSquaredTo(cloud.Position) > radiusSquared) continue;

                if (cloud.Slows) enemy.SlowTime = 0.6f;
                if (enemy.GasCooldown > 0f) continue;
                enemy.GasCooldown = 0.45f;
                DamageEnemy(enemy, cloud.Damage);
            }
        }

        if (level <= 0) return;

        _gasTimer -= dt;
        if (_gasTimer > 0f || _clouds.Count >= MaxClouds) return;

        _gasTimer = (level >= 4 ? 0.35f : 0.5f) / _rate;
        float life = 2.2f + (level >= 2 ? 1f : 0f) + (level >= 6 ? 1f : 0f);
        float radius = 11f + (level >= 3 ? 4f : 0f) + (level >= 6 ? 6f : 0f);
        float damage = 3f * (1f + 0.2f * (level - 1)) * damageScale;

        var sprite = new Sprite2D { Texture = Assets.Texture("gas"), ZIndex = -1, Scale = Vector2.One * (radius / 8f) };
        sprite.Position = _van.Position;
        _world.AddChild(sprite);
        _clouds.Add(new Cloud { Sprite = sprite, Position = _van.Position, Life = life, MaxLife = life, Radius = radius, Damage = damage, Slows = level >= 5 });
    }

    private void UpdateShots(float dt)
    {
        foreach (Shot shot in _shots)
        {
            if (shot.Dead) continue;
            shot.Position += shot.Velocity * dt;
            shot.Life -= dt;
            if (shot.Life <= 0f)
            {
                shot.Dead = true;
                continue;
            }

            if (shot.Spin != 0f) shot.Sprite.Rotation += shot.Spin * dt;
            shot.Sprite.Position = shot.Position;

            if (shot.Hostile)
            {
                float reach = shot.HitRadius;
                if (shot.Position.DistanceSquaredTo(_van.Position) < reach * reach && _invulnerable <= 0f)
                {
                    shot.Dead = true;
                    HurtVan(shot.Damage);
                    if (_state != State.Playing) return;
                }
                continue;
            }

            foreach (Enemy enemy in _enemies)
            {
                if (enemy.Dead) continue;
                float reach = enemy.Def.Radius + shot.HitRadius * 0.5f;
                if (enemy.Position.DistanceSquaredTo(shot.Position) > reach * reach) continue;
                if (shot.AlreadyHit.Contains(enemy)) continue;

                shot.AlreadyHit.Add(enemy);
                enemy.Knockback += shot.Velocity.Normalized() * (enemy.Def.Heavy ? 12f : 40f);
                DamageEnemy(enemy, shot.Damage);
                shot.Pierce--;
                if (shot.Pierce <= 0)
                {
                    shot.Dead = true;
                    break;
                }
            }
        }
    }

    // ---------------------------------------------------------------- Sinais (XP) e reparos

    private void SpawnGem(Vector2 position, float value)
    {
        if (_gems.Count >= MaxGems)
        {
            // Muitos no chão: funde o valor no sinal mais novo em vez de criar outro.
            Gem last = _gems[^1];
            last.Value += value;
            RetintGem(last);
            return;
        }

        var gem = new Gem { Position = position, Value = value, Sprite = new Sprite2D { Texture = Assets.Texture("gem"), ZIndex = 0 } };
        gem.Sprite.Position = position;
        RetintGem(gem);
        _world.AddChild(gem.Sprite);
        _gems.Add(gem);
    }

    private static void RetintGem(Gem gem)
    {
        gem.Sprite.Modulate = gem.Value >= 20f ? new Color(1f, 0.8f, 0.3f) : gem.Value >= 5f ? new Color(0.5f, 0.7f, 1f) : Colors.White;
        gem.Sprite.Scale = gem.Value >= 5f ? new Vector2(1.4f, 1.4f) : Vector2.One;
    }

    private void UpdateGems(float dt)
    {
        float magnetSquared = _magnet * _magnet;
        Vector2 vanPosition = _van.Position;
        bool collected = false;

        foreach (Gem gem in _gems)
        {
            float distanceSquared = gem.Position.DistanceSquaredTo(vanPosition);
            if (!gem.Attracted && distanceSquared < magnetSquared) gem.Attracted = true;
            if (!gem.Attracted) continue;

            gem.Position = gem.Position.MoveToward(vanPosition, (_van.Speed + 140f) * dt);
            gem.Sprite.Position = gem.Position;
            if (gem.Position.DistanceSquaredTo(vanPosition) < 36f)
            {
                gem.Dead = true;
                _xp += gem.Value * _xpMult;
                collected = true;
            }
        }

        if (collected)
            _audio.Play("UI/ui_hover.wav", -12f, 1.4f);
    }

    private void DropRepair(Vector2 position)
    {
        if (_pickups.Count >= MaxPickups) return;
        var pickup = new Pickup { Position = position, Sprite = new Sprite2D { Texture = Assets.Texture("repair"), ZIndex = 1 } };
        pickup.Sprite.Position = position;
        _world.AddChild(pickup.Sprite);
        _pickups.Add(pickup);
    }

    private void UpdatePickups()
    {
        float reach = Van.Radius + 6f;
        foreach (Pickup pickup in _pickups)
        {
            if (pickup.Position.DistanceSquaredTo(_van.Position) > reach * reach) continue;
            pickup.Dead = true;
            float heal = _maxHp * RepairHeal;
            _hp = Mathf.Min(_hp + heal, _maxHp);
            _hud.Toast("KIT DE REPARO!", UiStyle.Green, 1.5f);
            _audio.Play("SFX/buy.wav", -6f);
        }
    }

    // ---------------------------------------------------------------- Nível e upgrades

    private int XpToNextLevel()
    {
        return Mathf.RoundToInt(6f + 3.2f * _level + 0.12f * _level * _level);
    }

    private void LevelUp()
    {
        _xp -= XpToNextLevel();
        _level++;

        List<Offer> offers = BuildOffers();
        _state = State.ChoosingUpgrade;
        _audio.SetEngineRunning(false);
        _audio.Play("SFX/delivery_ok.wav", -6f);
        RefreshHud();
        _upgradeScreen.Open(offers, _levels);
    }

    private List<Offer> BuildOffers()
    {
        var pool = new List<UpgradeDef>();
        var fallbacks = new List<UpgradeDef>();
        foreach (UpgradeDef def in UpgradeCatalog.All)
        {
            if (def.Fallback) fallbacks.Add(def);
            else if (_levels[def.Id] < def.MaxLevel) pool.Add(def);
        }

        var offers = new List<Offer>();
        while (offers.Count < 3 && pool.Count > 0)
        {
            int index = _random.Next(pool.Count);
            offers.Add(new Offer { Def = pool[index], Rarity = Rarities.Roll(_random, _luck) });
            pool.RemoveAt(index);
        }

        // Sem opções de verdade (tudo no máximo): entram os anúncios sem fim.
        while (offers.Count < 3 && fallbacks.Count > 0)
        {
            int index = _random.Next(fallbacks.Count);
            offers.Add(new Offer { Def = fallbacks[index], Rarity = Rarities.Roll(_random, _luck) });
            fallbacks.RemoveAt(index);
        }
        return offers;
    }

    private void OnUpgradeChosen(Offer offer)
    {
        UpgradeDef def = offer.Def;
        float mult = Rarities.Mult[(int)offer.Rarity];

        _levels[def.Id]++;
        _power[def.Id] += mult;

        float oldMax = _maxHp;
        ApplyStats();

        switch (def.Id)
        {
            case UpgradeCatalog.Lataria:
                _hp = Mathf.Min(_hp + (_maxHp - oldMax), _maxHp);
                break;
            case UpgradeCatalog.Reparo:
                _hp = Mathf.Min(_hp + _maxHp * Mathf.Min(1f, def.PerPick * mult), _maxHp);
                break;
            case UpgradeCatalog.SinalForte:
                _xp += XpToNextLevel() * def.PerPick * mult;
                break;
            case UpgradeCatalog.Som when _levels[def.Id] == 1:
                _pulseTimer = 0.5f;
                break;
        }

        _hud.Toast($"{Rarities.Names[(int)offer.Rarity]}: {def.Name}", Rarities.Tint[(int)offer.Rarity], 1.8f);
        _audio.PlayUi("UI/ui_click.wav", -6f);
        _audio.SetEngineRunning(true);
        _state = State.Playing;
        RefreshHud();
    }

    private float Amount(string id)
    {
        return UpgradeCatalog.Find(id).PerPick * _power[id];
    }

    private void ApplyStats()
    {
        float motor = 1f + Amount(UpgradeCatalog.Motor);
        _van.MaxSpeed = 105f * motor;
        _van.Acceleration = 170f * motor;

        _maxHp = 100f + Amount(UpgradeCatalog.Lataria);
        if (_hp > _maxHp) _hp = _maxHp;

        _ramMult = 1f + Amount(UpgradeCatalog.Parachoque);
        _ramResist = Mathf.Min(0.6f, 0.06f * _power[UpgradeCatalog.Parachoque]);
        _magnet = BaseMagnet * (1f + Amount(UpgradeCatalog.Ima));
        _takenMult = 1f / (1f + Amount(UpgradeCatalog.Amortecedor));
        _regen = Amount(UpgradeCatalog.Alternador);
        _dmgMult = 1f + Amount(UpgradeCatalog.Calibragem) + Amount(UpgradeCatalog.Sucata);
        _rate = 1f + Amount(UpgradeCatalog.Velas);
        _xpMult = (1f + Amount(UpgradeCatalog.Radio)) * _diff.XpGain;
        _luck = 1f + Amount(UpgradeCatalog.Trevo);
    }

    // ---------------------------------------------------------------- Pausa

    private void BuildPauseMenu()
    {
        var layer = new CanvasLayer { Layer = 20 };
        AddChild(layer);
        _pause = new MenuPanel();
        layer.AddChild(_pause);

        var page = new MenuPanel.Page { Id = "pause", OnBack = ClosePause, DimAlpha = 0.7f };
        page.Title.Add(("PAUSA", 16, UiStyle.Green));
        page.Items.Add(MenuPanel.Button("Continuar", ClosePause));
        page.Items.Add(MenuPanel.Button("Configurações", () => _pause.Open("settings")));
        page.Items.Add(MenuPanel.Button("Salvar e sair", SaveAndQuit, "Guarda a partida; \"Continuar\" no menu retoma daqui."));
        page.Items.Add(MenuPanel.Button("Sair sem salvar", () => GetTree().ChangeSceneToFile(Session.MenuScene), "Volta ao menu sem guardar esta partida."));
        _pause.AddPage(page);
        _pause.AddPage(SettingsPages.Build(_pause, "pause", 0.85f));
        _pause.Close();
    }

    private void OpenPause()
    {
        _state = State.Paused;
        _audio.SetEngineRunning(false);
        _audio.PlayUi("UI/ui_pause.wav");
        _pause.Open("pause");
    }

    private void ClosePause()
    {
        if (_state != State.Paused) return;
        GameSettings.Save();
        _pause.Close();
        _state = State.Playing;
        _audio.SetEngineRunning(true);
    }

    private void SaveAndQuit()
    {
        var data = new RunData
        {
            Difficulty = _diffIndex, Level = _level, Kills = _kills, HordeIndex = _hordeIndex, EliteIndex = _eliteIndex,
            Time = _time, Xp = _xp, Hp = _hp, VanX = _van.Position.X, VanY = _van.Position.Y, Heading = _van.Heading,
        };
        foreach (UpgradeDef def in UpgradeCatalog.All)
        {
            data.Levels[def.Id] = _levels[def.Id];
            data.Power[def.Id] = _power[def.Id];
        }
        RunSave.Save(data);
        GetTree().ChangeSceneToFile(Session.MenuScene);
    }

    // ---------------------------------------------------------------- Tela e fim

    private void UpdateGround()
    {
        _ground.Position = (_van.Position / GroundTile).Floor() * GroundTile;
    }

    private void UpdateShake(float dt)
    {
        if (_shake <= 0f || !GameSettings.Shake)
        {
            _camera.Offset = Vector2.Zero;
            _shake = 0f;
            return;
        }
        _camera.Offset = new Vector2(
            Mathf.Round(((float)_random.NextDouble() - 0.5f) * 2f * _shake),
            Mathf.Round(((float)_random.NextDouble() - 0.5f) * 2f * _shake));
        _shake = Mathf.MoveToward(_shake, 0f, 12f * dt);
    }

    private void RemoveDead()
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (!_enemies[i].Dead) continue;
            _enemies[i].Sprite.QueueFree();
            _enemies.RemoveAt(i);
        }
        for (int i = _shots.Count - 1; i >= 0; i--)
        {
            if (!_shots[i].Dead) continue;
            _shots[i].Sprite.QueueFree();
            _shots.RemoveAt(i);
        }
        for (int i = _gems.Count - 1; i >= 0; i--)
        {
            if (!_gems[i].Dead) continue;
            _gems[i].Sprite.QueueFree();
            _gems.RemoveAt(i);
        }
        for (int i = _pickups.Count - 1; i >= 0; i--)
        {
            if (!_pickups[i].Dead) continue;
            _pickups[i].Sprite.QueueFree();
            _pickups.RemoveAt(i);
        }
    }

    private void RefreshHud()
    {
        _hud.SetXp(_xp / XpToNextLevel(), _level);
        _hud.SetHp(_hp, _maxHp);
        _hud.SetKills(_kills);

        if (_bossSpawned && !_bossDead && _boss != null)
        {
            _hud.SetClockText("CHEFÃO!");
            _hud.SetBoss(_boss.Hp / _boss.MaxHp, true, EnemyCatalog.Boss.Name);
        }
        else
        {
            int left = Mathf.CeilToInt(Mathf.Max(BossTime - _time, 0f));
            _hud.SetClockText($"{left / 60:00}:{left % 60:00}");
            _hud.SetBoss(0f, false);
        }
    }

    private void EndRun(State result)
    {
        _state = result;
        _camera.Offset = Vector2.Zero;
        _audio.SetEngineRunning(false);
        RunSave.Delete();
        _hud.SetBoss(0f, false);
        RefreshHud();

        int survived = (int)_time;
        string summary = $"Nível {_level}  -  {_kills} abates  -  {survived / 60:00}:{survived % 60:00}  -  {_diff.Name}\nEnter: jogar de novo   Esc: menu";
        if (result == State.Victory)
        {
            _audio.Play("SFX/delivery_ok.wav", -4f);
            _hud.ShowBanner("TURNO CUMPRIDO", $"O Zumbão da 96.6 foi para o ferro-velho.\n{summary}");
        }
        else
        {
            _audio.Play("SFX/game_over.wav", -4f);
            _hud.ShowBanner("A VAN PAROU", summary);
        }
    }
}
