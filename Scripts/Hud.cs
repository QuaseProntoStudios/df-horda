using Godot;

namespace DeadFrequencyHorda;

/// <summary>Barra de sinal (XP), vida da van, relógio, abates, barra do chefe e avisos rápidos.</summary>
public partial class Hud : CanvasLayer
{
    private const float ScreenWidth = 320f;
    private const float ScreenHeight = 180f;
    private const float HpBarWidth = 60f;
    private const float BossBarWidth = 160f;

    private ColorRect _xpFill;
    private ColorRect _hpFill;
    private ColorRect _hpFlash;
    private Label _levelLabel;
    private Label _hpLabel;
    private Label _timeLabel;
    private Label _killsLabel;
    private Label _toast;
    private float _toastTime;
    private float _flash;

    private Control _bossRoot;
    private ColorRect _bossFill;
    private Label _bossName;

    private ColorRect _bannerDim;
    private Label _bannerTitle;
    private Label _bannerSub;

    public override void _Ready()
    {
        Layer = 5;

        AddChild(new ColorRect { Color = UiStyle.Dark, Position = Vector2.Zero, Size = new Vector2(ScreenWidth, 4f) });
        _xpFill = new ColorRect { Color = UiStyle.Green, Position = Vector2.Zero, Size = new Vector2(0f, 4f) };
        AddChild(_xpFill);

        _levelLabel = UiStyle.MakeLabel("NV 1", UiStyle.Green);
        _levelLabel.Position = new Vector2(4f, 5f);
        AddChild(_levelLabel);

        AddChild(new ColorRect { Color = UiStyle.Dark, Position = new Vector2(4f, 18f), Size = new Vector2(HpBarWidth + 2f, 6f) });
        _hpFill = new ColorRect { Color = UiStyle.Red, Position = new Vector2(5f, 19f), Size = new Vector2(HpBarWidth, 4f) };
        AddChild(_hpFill);
        _hpFlash = new ColorRect { Color = new Color(1f, 1f, 1f, 0f), Position = new Vector2(4f, 18f), Size = new Vector2(HpBarWidth + 2f, 6f) };
        AddChild(_hpFlash);
        _hpLabel = UiStyle.MakeLabel("100", UiStyle.Red);
        _hpLabel.Position = new Vector2(HpBarWidth + 10f, 13f);
        AddChild(_hpLabel);

        _timeLabel = UiStyle.MakeLabel("00:00", UiStyle.Green);
        _timeLabel.Position = new Vector2(0f, 5f);
        _timeLabel.Size = new Vector2(ScreenWidth, 12f);
        _timeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(_timeLabel);

        _killsLabel = UiStyle.MakeLabel("0", UiStyle.Green);
        _killsLabel.Position = new Vector2(0f, 5f);
        _killsLabel.Size = new Vector2(ScreenWidth - 4f, 12f);
        _killsLabel.HorizontalAlignment = HorizontalAlignment.Right;
        AddChild(_killsLabel);

        // Aviso rápido no alto da tela (hordas, elite, kit de reparo...).
        _toast = UiStyle.MakeLabel("", Colors.White);
        _toast.Position = new Vector2(0f, 32f);
        _toast.Size = new Vector2(ScreenWidth, 12f);
        _toast.HorizontalAlignment = HorizontalAlignment.Center;
        _toast.Visible = false;
        AddChild(_toast);

        // Barra do chefe, embaixo.
        _bossRoot = new Control { Visible = false };
        AddChild(_bossRoot);
        float bossX = (ScreenWidth - BossBarWidth) * 0.5f;
        _bossRoot.AddChild(new ColorRect { Color = UiStyle.Dark, Position = new Vector2(bossX - 1f, 163f), Size = new Vector2(BossBarWidth + 2f, 8f) });
        _bossFill = new ColorRect { Color = UiStyle.Red, Position = new Vector2(bossX, 164f), Size = new Vector2(BossBarWidth, 6f) };
        _bossRoot.AddChild(_bossFill);
        _bossName = UiStyle.MakeLabel("", UiStyle.Red);
        _bossName.Position = new Vector2(0f, 152f);
        _bossName.Size = new Vector2(ScreenWidth, 12f);
        _bossName.HorizontalAlignment = HorizontalAlignment.Center;
        _bossRoot.AddChild(_bossName);

        _bannerDim = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.6f),
            Position = Vector2.Zero,
            Size = new Vector2(ScreenWidth, ScreenHeight),
            Visible = false,
        };
        AddChild(_bannerDim);

        _bannerTitle = UiStyle.MakeTitle("", UiStyle.Green, 24);
        _bannerTitle.Position = new Vector2(0f, 50f);
        _bannerTitle.Size = new Vector2(ScreenWidth, 26f);
        _bannerTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _bannerTitle.Visible = false;
        AddChild(_bannerTitle);

        _bannerSub = UiStyle.MakeLabel("", UiStyle.Green);
        _bannerSub.Position = new Vector2(0f, 82f);
        _bannerSub.Size = new Vector2(ScreenWidth, 60f);
        _bannerSub.HorizontalAlignment = HorizontalAlignment.Center;
        _bannerSub.Visible = false;
        AddChild(_bannerSub);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_toastTime > 0f)
        {
            _toastTime -= dt;
            if (_toastTime <= 0f) _toast.Visible = false;
            else _toast.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(_toastTime * 2f, 0f, 1f));
        }
        if (_flash > 0f)
        {
            _flash = Mathf.Max(0f, _flash - dt * 3f);
            _hpFlash.Color = new Color(1f, 1f, 1f, _flash * 0.8f);
        }
    }

    public void SetXp(float fraction, int level)
    {
        _xpFill.Size = new Vector2(ScreenWidth * Mathf.Clamp(fraction, 0f, 1f), 4f);
        _levelLabel.Text = $"NV {level}";
    }

    public void SetHp(float hp, float maxHp)
    {
        float fraction = maxHp > 0f ? Mathf.Clamp(hp / maxHp, 0f, 1f) : 0f;
        _hpFill.Size = new Vector2(HpBarWidth * fraction, 4f);
        _hpLabel.Text = Mathf.CeilToInt(Mathf.Max(hp, 0f)).ToString();
    }

    /// <summary>Pisca a barra de vida quando a van apanha.</summary>
    public void FlashHp()
    {
        _flash = 1f;
    }

    public void SetClockText(string text)
    {
        _timeLabel.Text = text;
    }

    public void SetKills(int kills)
    {
        _killsLabel.Text = $"ABATES {kills}";
    }

    public void Toast(string text, Color color, float seconds = 2.5f)
    {
        _toast.Text = text;
        _toast.AddThemeColorOverride("font_color", color);
        _toast.Modulate = Colors.White;
        _toast.Visible = true;
        _toastTime = seconds;
    }

    public void SetBoss(float fraction, bool visible, string name = "")
    {
        _bossRoot.Visible = visible;
        if (!visible) return;
        _bossName.Text = name;
        _bossFill.Size = new Vector2(BossBarWidth * Mathf.Clamp(fraction, 0f, 1f), 6f);
    }

    public void ShowBanner(string title, string subtitle)
    {
        _bannerTitle.Text = title;
        _bannerSub.Text = subtitle;
        _bannerDim.Visible = true;
        _bannerTitle.Visible = true;
        _bannerSub.Visible = true;
    }

    public void HideBanner()
    {
        _bannerDim.Visible = false;
        _bannerTitle.Visible = false;
        _bannerSub.Visible = false;
    }
}
