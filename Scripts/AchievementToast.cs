using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>Aviso de conquista no canto da tela: entra, fica uns segundos e sai. Vários avisos viram fila.</summary>
public partial class AchievementToast : CanvasLayer
{
    private const float Shown = 4f;
    private const float SlideTime = 0.35f;
    private const float BoxW = 250f;
    private const float BoxH = 44f;

    private readonly Queue<AchievementDef> _queue = new();
    private Control _box;
    private Label _kicker;
    private Label _name;
    private Label _text;
    private AudioStreamPlayer _sound;
    private float _age = -1f;

    public static AchievementToast Create(Node parent)
    {
        var toast = new AchievementToast();
        parent.AddChild(toast);
        return toast;
    }

    public override void _Ready()
    {
        Layer = 30;
        ProcessMode = ProcessModeEnum.Always;

        _box = new Control { Size = new Vector2(BoxW, BoxH), Visible = false };
        AddChild(_box);

        var panel = new Panel { Size = new Vector2(BoxW, BoxH) };
        panel.AddThemeStyleboxOverride("panel", UiStyle.Box(new Color(0.03f, 0.05f, 0.02f, 0.96f), new Color(1f, 0.78f, 0.25f), 2));
        _box.AddChild(panel);

        _kicker = UiStyle.MakeLabel("CONQUISTA DESBLOQUEADA", new Color(1f, 0.78f, 0.25f));
        _kicker.Position = new Vector2(6f, 2f);
        _box.AddChild(_kicker);

        _name = UiStyle.MakeTitle("", Colors.White, UiStyle.FontSize);
        _name.Position = new Vector2(6f, 13f);
        _name.Size = new Vector2(BoxW - 12f, 10f);
        _box.AddChild(_name);

        _text = UiStyle.MakeLabel("", UiStyle.Hint);
        _text.Position = new Vector2(6f, 25f);
        _text.Size = new Vector2(BoxW - 12f, 18f);
        _text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _box.AddChild(_text);

        _sound = new AudioStreamPlayer { Bus = GameSettings.BusUi, VolumeDb = -2f, Stream = Assets.Stream("UI/ui_start.wav") };
        AddChild(_sound);

        Achievements.Unlocked += OnUnlocked;
    }

    public override void _ExitTree()
    {
        Achievements.Unlocked -= OnUnlocked;
    }

    private void OnUnlocked(AchievementDef def)
    {
        _queue.Enqueue(def);
    }

    public override void _Process(double delta)
    {
        if (_age < 0f)
        {
            if (_queue.Count == 0) return;
            AchievementDef def = _queue.Dequeue();
            _name.Text = def.Name;
            _text.Text = def.Description;
            _kicker.Text = def.Reward > 0 ? $"CONQUISTA DESBLOQUEADA  +{def.Reward} FICHAS" : "CONQUISTA DESBLOQUEADA";
            _age = 0f;
            _box.Visible = true;
            _sound.Play();
        }

        _age += (float)delta;
        float slide = Mathf.Clamp(_age / SlideTime, 0f, 1f);
        float leave = Mathf.Clamp((_age - (Shown - SlideTime)) / SlideTime, 0f, 1f);
        float visible = Mathf.Ease(slide, -2f) * (1f - leave);
        _box.Position = new Vector2(320f - (BoxW + 4f) * visible, 22f);

        if (_age >= Shown)
        {
            _age = -1f;
            _box.Visible = false;
        }
    }
}
