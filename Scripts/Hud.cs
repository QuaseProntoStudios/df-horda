using Godot;

namespace DeadFrequencyHorda;

/// <summary>Barra de sinal (XP), vida da van, relógio do turno e contador de abates.</summary>
public partial class Hud : CanvasLayer
{
    private const float ScreenWidth = 320f;
    private const float ScreenHeight = 180f;
    private const float HpBarWidth = 60f;

    private ColorRect _xpFill;
    private ColorRect _hpFill;
    private Label _levelLabel;
    private Label _hpLabel;
    private Label _timeLabel;
    private Label _killsLabel;
    private ColorRect _bannerDim;
    private Label _bannerTitle;
    private Label _bannerSub;

    public override void _Ready()
    {
        Layer = 5;

        // XP: faixa fina no topo da tela inteira.
        AddChild(new ColorRect { Color = UiStyle.Dark, Position = Vector2.Zero, Size = new Vector2(ScreenWidth, 4f) });
        _xpFill = new ColorRect { Color = UiStyle.Green, Position = Vector2.Zero, Size = new Vector2(0f, 4f) };
        AddChild(_xpFill);

        _levelLabel = UiStyle.MakeLabel("NV 1", UiStyle.Green);
        _levelLabel.Position = new Vector2(4f, 5f);
        AddChild(_levelLabel);

        // Vida da van.
        AddChild(new ColorRect { Color = UiStyle.Dark, Position = new Vector2(4f, 18f), Size = new Vector2(HpBarWidth + 2f, 6f) });
        _hpFill = new ColorRect { Color = UiStyle.Red, Position = new Vector2(5f, 19f), Size = new Vector2(HpBarWidth, 4f) };
        AddChild(_hpFill);
        _hpLabel = UiStyle.MakeLabel("100", UiStyle.Red);
        _hpLabel.Position = new Vector2(HpBarWidth + 10f, 14f);
        AddChild(_hpLabel);

        _timeLabel = UiStyle.MakeLabel("15:00", UiStyle.Green);
        _timeLabel.Position = new Vector2(0f, 5f);
        _timeLabel.Size = new Vector2(ScreenWidth, 12f);
        _timeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(_timeLabel);

        _killsLabel = UiStyle.MakeLabel("0", UiStyle.Green);
        _killsLabel.Position = new Vector2(0f, 5f);
        _killsLabel.Size = new Vector2(ScreenWidth - 4f, 12f);
        _killsLabel.HorizontalAlignment = HorizontalAlignment.Right;
        AddChild(_killsLabel);

        // Faixa central para pausa, fim de jogo e vitória.
        _bannerDim = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.6f),
            Position = Vector2.Zero,
            Size = new Vector2(ScreenWidth, ScreenHeight),
            Visible = false,
        };
        AddChild(_bannerDim);

        _bannerTitle = UiStyle.MakeLabel("", UiStyle.Green);
        _bannerTitle.AddThemeFontSizeOverride("font_size", 16);
        _bannerTitle.Position = new Vector2(0f, 62f);
        _bannerTitle.Size = new Vector2(ScreenWidth, 24f);
        _bannerTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _bannerTitle.Visible = false;
        AddChild(_bannerTitle);

        _bannerSub = UiStyle.MakeLabel("", UiStyle.Green);
        _bannerSub.Position = new Vector2(0f, 90f);
        _bannerSub.Size = new Vector2(ScreenWidth, 40f);
        _bannerSub.HorizontalAlignment = HorizontalAlignment.Center;
        _bannerSub.Visible = false;
        AddChild(_bannerSub);
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

    public void SetTimeLeft(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(seconds, 0f));
        _timeLabel.Text = $"{total / 60:00}:{total % 60:00}";
    }

    public void SetKills(int kills)
    {
        _killsLabel.Text = $"ABATES {kills}";
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
