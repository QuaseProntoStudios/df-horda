using System;
using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>
/// A partida inteira: horda, armas, sinais (XP), upgrades e fim de jogo.
/// Inimigos, projéteis e sinais são objetos simples com um Sprite2D cada, atualizados
/// aqui por distância (sem física do Godot), o que aguenta centenas na tela.
/// </summary>
public partial class Game : Node2D
{
    private enum State { Playing, ChoosingUpgrade, Paused, GameOver, Victory }

    private sealed class Enemy
    {
        public Sprite2D Sprite;
        public Vector2 Position;
        public Vector2 Knockback;
        public float Hp;
        public float Speed;
        public float RamCooldown;
        public float FlashTime;
        public float AnimTime;
        public bool Dead;
    }

    private sealed class Bullet
    {
        public Sprite2D Sprite;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Damage;
        public float Life;
        public int Pierce;
        public readonly List<Enemy> AlreadyHit = new();
        public bool Dead;
    }

    private sealed class Gem
    {
        public Sprite2D Sprite;
        public Vector2 Position;
        public bool Attracted;
        public bool Dead;
    }

    // ---- Regras da partida ----
    private const float RunLength = 15f * 60f;
    private const int MaxEnemies = 320;
    private const float SpawnRadius = 205f;
    private const float LeashRadius = 330f;
    private const float EnemyRadius = 5f;
    private const float EnemyContactDamage = 6f;
    private const float VanInvulnerability = 0.6f;
    private const float RamMinSpeed = 45f;
    private const float RamBaseDamage = 10f;
    private const float BulletSpeed = 190f;
    private const float BulletRange = 150f;
    private const float BaseMagnet = 22f;
    private const float GroundTile = 64f;

    private State _state = State.Playing;
    private readonly Random _random = new();

    private Van _van;
    private Sprite2D _ground;
    private Camera2D _camera;
    private Node2D _world;
    private Hud _hud;
    private UpgradeScreen _upgradeScreen;
    private GameAudio _audio;

    private readonly List<Enemy> _enemies = new();
    private readonly List<Bullet> _bullets = new();
    private readonly List<Gem> _gems = new();
    private Dictionary<string, int> _levels;

    private float _time;
    private float _spawnTimer;
    private float _antennaTimer;
    private float _pulseTimer;
    private float _hp = 100f;
    private float _maxHp = 100f;
    private float _invulnerable;
    private float _ramSoundCooldown;
    private float _shake;
    private int _xp;
    private int _level = 1;
    private int _kills;

    public override void _Ready()
    {
        _levels = UpgradeCatalog.StartingLevels();

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

        ApplyStats();
        RefreshHud();
    }

    public override void _Process(double delta)
    {
        if (_state != State.Playing) return;
        float dt = Mathf.Min((float)delta, 0.05f);

        _time += dt;
        if (_time >= RunLength)
        {
            EndRun(State.Victory);
            return;
        }

        _van.Tick(dt);
        _audio.SetEngine(_van.Speed / _van.MaxSpeed);
        UpdateGround();
        UpdateShake(dt);

        if (_invulnerable > 0f) _invulnerable -= dt;
        if (_ramSoundCooldown > 0f) _ramSoundCooldown -= dt;

        SpawnEnemies(dt);
        UpdateEnemies(dt);
        if (_state != State.Playing) return; // a van pode ter parado neste quadro

        UpdateAntenna(dt);
        UpdatePulse(dt);
        UpdateBullets(dt);
        UpdateGems(dt);
        RemoveDead();
        RefreshHud();

        if (_state == State.Playing && _xp >= XpToNextLevel())
            LevelUp();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;

        if (key.Keycode == Key.Escape || key.PhysicalKeycode == Key.P)
        {
            if (_state == State.Playing)
            {
                _state = State.Paused;
                _audio.SetEngineRunning(false);
                _audio.Play("UI/ui_pause.wav");
                _hud.ShowBanner("PAUSA", "Esc para voltar");
            }
            else if (_state == State.Paused)
            {
                _state = State.Playing;
                _audio.SetEngineRunning(true);
                _hud.HideBanner();
            }
        }
        else if (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter || key.Keycode == Key.Space)
        {
            if (_state == State.GameOver || _state == State.Victory)
                GetTree().ReloadCurrentScene();
        }
    }

    // ---------------------------------------------------------------- Horda

    private void SpawnEnemies(float dt)
    {
        float progress = _time / RunLength;
        _spawnTimer -= dt;
        if (_spawnTimer > 0f) return;

        _spawnTimer = Mathf.Lerp(0.7f, 0.07f, progress);
        int batch = 1 + (int)(_time / 120f);
        for (int i = 0; i < batch && _enemies.Count < MaxEnemies; i++)
            SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        var enemy = new Enemy
        {
            Position = RingPosition(),
            Hp = 5f + _time * 0.06f,
            Speed = 26f + (float)_random.NextDouble() * 10f + _time * 0.012f,
            AnimTime = (float)_random.NextDouble(),
            Sprite = new Sprite2D { Texture = Assets.Texture("zombie"), Hframes = 2, ZIndex = 1 },
        };
        enemy.Sprite.Position = enemy.Position;
        _world.AddChild(enemy.Sprite);
        _enemies.Add(enemy);
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
        float touch = Van.Radius + EnemyRadius;
        int ramLevel = _levels[UpgradeCatalog.Parachoque];
        int count = _enemies.Count;

        for (int i = 0; i < count; i++)
        {
            Enemy enemy = _enemies[i];
            if (enemy.Dead) continue;

            Vector2 toVan = vanPosition - enemy.Position;
            float distance = toVan.Length();

            // Ficou muito para trás: volta para o anel em vez de sumir.
            if (distance > LeashRadius)
            {
                enemy.Position = RingPosition();
                enemy.Knockback = Vector2.Zero;
                toVan = vanPosition - enemy.Position;
                distance = toVan.Length();
            }

            Vector2 direction = distance > 0.01f ? toVan / distance : Vector2.Zero;
            enemy.Position += (direction * enemy.Speed + enemy.Knockback) * dt;
            enemy.Knockback = enemy.Knockback.MoveToward(Vector2.Zero, 320f * dt);

            // Empurra de leve dois vizinhos da lista para a horda não virar um ponto só.
            Separate(enemy, _enemies[(i + 1) % count]);
            Separate(enemy, _enemies[(i + 7) % count]);

            if (enemy.RamCooldown > 0f) enemy.RamCooldown -= dt;
            if (enemy.FlashTime > 0f) enemy.FlashTime -= dt;

            if (distance < touch)
            {
                if (vanSpeed > RamMinSpeed)
                {
                    if (enemy.RamCooldown <= 0f)
                    {
                        enemy.RamCooldown = 0.35f;
                        float damage = RamBaseDamage * (1f + 0.4f * ramLevel) * (vanSpeed / 100f);
                        enemy.Knockback = _van.Velocity.Normalized() * 130f - direction * 50f;
                        _van.Velocity *= Mathf.Min(0.90f + 0.02f * ramLevel, 0.99f);
                        DamageEnemy(enemy, damage);
                        _shake = Mathf.Max(_shake, 1.5f);
                        if (_ramSoundCooldown <= 0f)
                        {
                            _ramSoundCooldown = 0.08f;
                            _audio.Play("SFX/impact_box.wav", -8f, 0.9f + (float)_random.NextDouble() * 0.3f);
                        }
                    }
                }
                else if (_invulnerable <= 0f)
                {
                    HurtVan(EnemyContactDamage);
                    if (_state != State.Playing) return;
                }
            }

            enemy.AnimTime += dt;
            enemy.Sprite.Position = enemy.Position;
            enemy.Sprite.Frame = (int)(enemy.AnimTime * 4f) % 2;
            enemy.Sprite.FlipH = direction.X < 0f;
            enemy.Sprite.Modulate = enemy.FlashTime > 0f ? new Color(3f, 3f, 3f) : Colors.White;
        }
    }

    private static void Separate(Enemy a, Enemy b)
    {
        if (ReferenceEquals(a, b) || b.Dead) return;
        Vector2 offset = a.Position - b.Position;
        float distanceSquared = offset.LengthSquared();
        float minimum = EnemyRadius * 2f;
        if (distanceSquared >= minimum * minimum || distanceSquared < 0.0001f) return;

        float distance = Mathf.Sqrt(distanceSquared);
        Vector2 push = offset / distance * ((minimum - distance) * 0.5f);
        a.Position += push;
        b.Position -= push;
    }

    private void DamageEnemy(Enemy enemy, float damage)
    {
        if (enemy.Dead) return;
        enemy.Hp -= damage;
        enemy.FlashTime = 0.08f;
        if (enemy.Hp > 0f) return;

        enemy.Dead = true;
        _kills++;
        SpawnGem(enemy.Position);
    }

    private void HurtVan(float damage)
    {
        _hp -= damage;
        _invulnerable = VanInvulnerability;
        _shake = Mathf.Max(_shake, 3f);
        _van.Flash();
        _audio.Play("SFX/crash_light.wav", -5f);
        if (_hp <= 0f)
        {
            _hp = 0f;
            EndRun(State.GameOver);
        }
    }

    // ---------------------------------------------------------------- Armas

    private void UpdateAntenna(float dt)
    {
        int level = _levels[UpgradeCatalog.Antena];
        if (level <= 0) return;

        _antennaTimer -= dt;
        if (_antennaTimer > 0f) return;

        float rate = 1f + 0.2f * (level - 1) + (level >= 6 ? 0.3f : 0f);
        float damage = 6f * (1f + 0.25f * (level - 1) + (level >= 6 ? 0.25f : 0f));
        int shots = level >= 5 ? 3 : level >= 3 ? 2 : 1;
        int pierce = level >= 4 ? 2 : 1;

        var targets = new List<Enemy>(shots);
        for (int shot = 0; shot < shots; shot++)
        {
            Enemy best = null;
            float bestDistance = BulletRange * BulletRange;
            foreach (Enemy enemy in _enemies)
            {
                if (enemy.Dead || targets.Contains(enemy)) continue;
                float distance = enemy.Position.DistanceSquaredTo(_van.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = enemy;
                }
            }
            if (best == null) break;
            targets.Add(best);
        }

        if (targets.Count == 0)
        {
            _antennaTimer = 0.1f; // ninguém no alcance: tenta de novo logo
            return;
        }

        _antennaTimer = 0.9f / rate;
        foreach (Enemy target in targets)
        {
            Vector2 direction = (target.Position - _van.Position).Normalized();
            var bullet = new Bullet
            {
                Position = _van.Position,
                Velocity = direction * BulletSpeed,
                Damage = damage,
                Life = BulletRange / BulletSpeed * 1.3f,
                Pierce = pierce,
                Sprite = new Sprite2D { Texture = Assets.Texture("wave"), ZIndex = 3, Rotation = direction.Angle() },
            };
            bullet.Sprite.Position = bullet.Position;
            _world.AddChild(bullet.Sprite);
            _bullets.Add(bullet);
        }
    }

    private void UpdateBullets(float dt)
    {
        float hitDistance = EnemyRadius + 3f;
        float hitDistanceSquared = hitDistance * hitDistance;

        foreach (Bullet bullet in _bullets)
        {
            bullet.Position += bullet.Velocity * dt;
            bullet.Life -= dt;
            if (bullet.Life <= 0f)
            {
                bullet.Dead = true;
                continue;
            }

            foreach (Enemy enemy in _enemies)
            {
                if (enemy.Dead) continue;
                if (enemy.Position.DistanceSquaredTo(bullet.Position) > hitDistanceSquared) continue;
                if (bullet.AlreadyHit.Contains(enemy)) continue;

                bullet.AlreadyHit.Add(enemy);
                enemy.Knockback += bullet.Velocity.Normalized() * 40f;
                DamageEnemy(enemy, bullet.Damage);
                bullet.Pierce--;
                if (bullet.Pierce <= 0)
                {
                    bullet.Dead = true;
                    break;
                }
            }

            bullet.Sprite.Position = bullet.Position;
        }
    }

    private void UpdatePulse(float dt)
    {
        int level = _levels[UpgradeCatalog.Som];
        if (level <= 0) return;

        _pulseTimer -= dt;
        if (_pulseTimer > 0f) return;

        _pulseTimer = level >= 3 ? 3.2f : 4f;
        float radius = 40f + 9f * (level - 1);
        float damage = 8f * (1f + 0.3f * (level - 1));
        float push = level >= 5 ? 260f : 160f;

        Vector2 center = _van.Position;
        foreach (Enemy enemy in _enemies)
        {
            if (enemy.Dead) continue;
            Vector2 offset = enemy.Position - center;
            float distance = offset.Length();
            if (distance > radius) continue;

            enemy.Knockback = (distance > 0.01f ? offset / distance : Vector2.Right) * push;
            DamageEnemy(enemy, damage);
        }

        _world.AddChild(PulseFx.Create(center, radius));
        _audio.Play("SFX/bus_horn.wav", -10f, 1.15f);
    }

    // ---------------------------------------------------------------- Sinais (XP)

    private void SpawnGem(Vector2 position)
    {
        var gem = new Gem
        {
            Position = position,
            Sprite = new Sprite2D { Texture = Assets.Texture("gem"), ZIndex = 0 },
        };
        gem.Sprite.Position = position;
        _world.AddChild(gem.Sprite);
        _gems.Add(gem);
    }

    private void UpdateGems(float dt)
    {
        float magnet = BaseMagnet * (1f + 0.5f * _levels[UpgradeCatalog.Ima]);
        float magnetSquared = magnet * magnet;
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
                _xp++;
                collected = true;
            }
        }

        if (collected)
            _audio.Play("UI/ui_hover.wav", -12f, 1.4f);
    }

    private int XpToNextLevel()
    {
        return 4 + _level * 3;
    }

    private void LevelUp()
    {
        _xp -= XpToNextLevel();
        _level++;

        var available = new List<UpgradeDef>();
        foreach (UpgradeDef def in UpgradeCatalog.All)
        {
            if (_levels[def.Id] < def.MaxLevel) available.Add(def);
        }

        if (available.Count == 0)
        {
            // Tudo no máximo: o nível vira conserto.
            _hp = Mathf.Min(_hp + 20f, _maxHp);
            RefreshHud();
            return;
        }

        var offers = new List<UpgradeDef>();
        while (offers.Count < 3 && available.Count > 0)
        {
            int index = _random.Next(available.Count);
            offers.Add(available[index]);
            available.RemoveAt(index);
        }

        _state = State.ChoosingUpgrade;
        _audio.SetEngineRunning(false);
        _audio.Play("SFX/delivery_ok.wav", -6f);
        RefreshHud();
        _upgradeScreen.Open(offers, _levels);
    }

    private void OnUpgradeChosen(UpgradeDef def)
    {
        _levels[def.Id]++;
        if (def.Id == UpgradeCatalog.Lataria)
        {
            _maxHp += 20f;
            _hp = Mathf.Min(_hp + 30f, _maxHp);
        }
        else if (def.Id == UpgradeCatalog.Som && _levels[def.Id] == 1)
        {
            _pulseTimer = 0.5f;
        }

        ApplyStats();
        _audio.Play("UI/ui_click.wav", -6f);
        _audio.SetEngineRunning(true);
        _state = State.Playing;
        RefreshHud();
    }

    private void ApplyStats()
    {
        float motor = 1f + 0.10f * _levels[UpgradeCatalog.Motor];
        _van.MaxSpeed = 105f * motor;
        _van.Acceleration = 170f * motor;
    }

    // ---------------------------------------------------------------- Tela e fim

    private void UpdateGround()
    {
        // O chão é um retângulo de textura repetida que anda de ladrilho em ladrilho com a van.
        _ground.Position = (_van.Position / GroundTile).Floor() * GroundTile;
    }

    private void UpdateShake(float dt)
    {
        if (_shake <= 0f)
        {
            _camera.Offset = Vector2.Zero;
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
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            if (!_bullets[i].Dead) continue;
            _bullets[i].Sprite.QueueFree();
            _bullets.RemoveAt(i);
        }
        for (int i = _gems.Count - 1; i >= 0; i--)
        {
            if (!_gems[i].Dead) continue;
            _gems[i].Sprite.QueueFree();
            _gems.RemoveAt(i);
        }
    }

    private void RefreshHud()
    {
        _hud.SetXp((float)_xp / XpToNextLevel(), _level);
        _hud.SetHp(_hp, _maxHp);
        _hud.SetTimeLeft(RunLength - _time);
        _hud.SetKills(_kills);
    }

    private void EndRun(State result)
    {
        _state = result;
        _camera.Offset = Vector2.Zero;
        _audio.SetEngineRunning(false);
        RefreshHud();

        int survived = (int)_time;
        string summary = $"Nível {_level}  -  {_kills} abates  -  {survived / 60:00}:{survived % 60:00}\nEnter para rodar de novo";
        if (result == State.Victory)
        {
            _audio.Play("SFX/delivery_ok.wav", -4f);
            _hud.ShowBanner("TURNO CUMPRIDO", summary);
        }
        else
        {
            _audio.Play("SFX/game_over.wav", -4f);
            _hud.ShowBanner("A VAN PAROU", summary);
        }
    }
}
