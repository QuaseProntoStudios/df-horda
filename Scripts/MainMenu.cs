using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>Menu principal: Jogar, Continuar, Configurações e Sair, com a van e a horda ao fundo.</summary>
public partial class MainMenu : Node2D
{
    private sealed class Walker
    {
        public Sprite2D Sprite;
        public Vector2 Position;
        public float Speed;
        public float Time;
    }

    private const float GroundTile = 64f;
    private const int WalkerCount = 9;

    private readonly RandomNumberGenerator _random = new();
    private readonly List<Walker> _walkers = new();
    private Sprite2D _ground;
    private Sprite2D _van;
    private Vector2 _scroll;
    private float _time;
    private MenuPanel _menu;
    private bool _starting;
    private AchievementsScreen _achievements;
    private WorkshopScreen _workshop;

    public override void _Ready()
    {
        GameSettings.Init();
        _random.Randomize();

        BuildBackdrop();
        BuildMusic();
        BuildMenu();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;

        _scroll += new Vector2(-16f, -10f) * dt;
        _ground.Position = new Vector2(160f, 90f) + new Vector2(-Mathf.PosMod(-_scroll.X, GroundTile), -Mathf.PosMod(-_scroll.Y, GroundTile));
        _van.Rotation = Mathf.Sin(_time * 1.4f) * 0.12f;
        _van.Position = new Vector2(160f + Mathf.Sin(_time * 0.7f) * 6f, 100f);

        foreach (Walker walker in _walkers)
        {
            Vector2 toVan = _van.Position - walker.Position;
            if (toVan.Length() < 18f)
            {
                Respawn(walker);
                continue;
            }

            walker.Position += toVan.Normalized() * walker.Speed * dt;
            walker.Time += dt;
            walker.Sprite.Position = walker.Position;
            walker.Sprite.Frame = (int)(walker.Time * 4f) % 2;
            walker.Sprite.FlipH = toVan.X < 0f;
        }
    }

    private void BuildBackdrop()
    {
        _ground = new Sprite2D
        {
            Texture = Assets.Texture("ground"),
            TextureRepeat = TextureRepeatEnum.Enabled,
            RegionEnabled = true,
            RegionRect = new Rect2(0f, 0f, GroundTile * 8f, GroundTile * 6f),
            ZIndex = -10,
        };
        AddChild(_ground);

        _van = new Sprite2D { Texture = Assets.Texture("van"), ZIndex = 2 };
        AddChild(_van);

        for (int i = 0; i < WalkerCount; i++)
        {
            var walker = new Walker
            {
                Sprite = new Sprite2D { Texture = Assets.Texture("zombie"), Hframes = 2, ZIndex = 1 },
                Speed = 14f + _random.RandfRange(0f, 12f),
                Time = _random.Randf(),
            };
            AddChild(walker.Sprite);
            Respawn(walker);
            // Espalha no começo para a tela não nascer vazia.
            walker.Position = new Vector2(160f, 100f) + Vector2.Right.Rotated(_random.Randf() * Mathf.Tau) * _random.RandfRange(40f, 190f);
            _walkers.Add(walker);
        }
    }

    private void Respawn(Walker walker)
    {
        walker.Position = new Vector2(160f, 100f) + Vector2.Right.Rotated(_random.Randf() * Mathf.Tau) * 200f;
        walker.Sprite.Position = walker.Position;
    }

    private void BuildMusic()
    {
        AudioStream stream = Assets.Stream("Music/music_menu.ogg");
        if (stream == null) return;

        var player = new AudioStreamPlayer { Stream = stream, Bus = GameSettings.BusMusic, VolumeDb = -4f };
        AddChild(player);
        player.Finished += () => player.Play();
        player.Play();
    }

    private void BuildMenu()
    {
        var layer = new CanvasLayer { Layer = 5 };
        AddChild(layer);
        _menu = new MenuPanel();
        layer.AddChild(_menu);

        var main = new MenuPanel.Page { Id = "main", DimAlpha = 0.55f };
        main.Title.Add(("DEAD FREQUENCY", 14, UiStyle.Green));
        main.Title.Add(("H O R D A", 20, new Color(0.7f, 1f, 0.75f)));
        main.Title.Add(("96.6 FM", 10, UiStyle.Hint));
        main.Items.Add(MenuPanel.Button("Jogar", () => _menu.Open("difficulty"), "Começa uma partida nova."));
        main.Items.Add(new MenuPanel.Item
        {
            Kind = MenuPanel.Kind.Button, Text = "Continuar", OnActivate = () => StartRun(Session.Difficulty, true),
            Enabled = () => RunSave.Exists,
            Hint = () => RunSave.Exists ? RunSave.Summary() : "Nenhuma partida salva. Use \"Salvar e sair\" na pausa.",
        });
        main.Items.Add(MenuPanel.Button("Oficina", OpenWorkshop, "Gaste fichas em upgrades permanentes."));
        main.Items.Add(MenuPanel.Button("Conquistas", OpenAchievements, "Veja o que você já desbloqueou."));
        main.Items.Add(MenuPanel.Button("Configurações", () => _menu.Open("settings"), "Volumes, tremor de tela e tela cheia."));
        main.Items.Add(MenuPanel.Button("Sair", () => GetTree().Quit()));
        _menu.AddPage(main);

        var difficulty = new MenuPanel.Page { Id = "difficulty", OnBack = () => _menu.Open("main"), StartIndex = Session.Difficulty };
        difficulty.Title.Add(("DIFICULDADE", 16, UiStyle.Green));
        for (int i = 0; i < Difficulties.All.Length; i++)
        {
            int index = i;
            DifficultyDef def = Difficulties.All[i];
            difficulty.Items.Add(MenuPanel.Button(def.Name, () => StartRun(index, false), def.Description));
        }
        difficulty.Items.Add(MenuPanel.Button("Voltar", () => _menu.Open("main")));
        _menu.AddPage(difficulty);

        _menu.AddPage(SettingsPages.Build(_menu, "main", 0.78f));

        _achievements = new AchievementsScreen();
        _achievements.Closed += () => _menu.Visible = true;
        layer.AddChild(_achievements);
        _workshop = new WorkshopScreen();
        _workshop.Closed += () => _menu.Visible = true;
        layer.AddChild(_workshop);
        AchievementToast.Create(this);

        var footer = UiStyle.MakeLabel($"v{GameVersion.Current}  -  GPL-3.0  -  teuzin0dev", UiStyle.GreenDim);
        footer.Position = new Vector2(4f, 168f);
        layer.AddChild(footer);

        // Se existe partida salva, o cursor já cai em "Continuar".
        if (RunSave.Exists) main.StartIndex = 1;
        _menu.Open("main");
    }

    private void OpenAchievements()
    {
        _menu.Visible = false;
        _achievements.Open();
    }

    private void OpenWorkshop()
    {
        _menu.Visible = false;
        _workshop.Open();
    }

    private void StartRun(int difficulty, bool continueRun)
    {
        if (_starting) return;
        _starting = true;

        Session.Difficulty = difficulty;
        Session.ContinueRun = continueRun;
        GameSettings.Difficulty = difficulty;
        GameSettings.Save();
        GetTree().ChangeSceneToFile(Session.GameScene);
    }
}
