using System;
using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>
/// Tela de subir de nível: três anúncios da rádio, cada um com a cor da sua raridade.
/// Escolha com clique, teclas 1-3, setas + Enter ou controle.
/// </summary>
public partial class UpgradeScreen : CanvasLayer
{
    private const int Slots = 3;
    private const float SlotW = 98f;
    private const float SlotH = 126f;
    private const float SlotGap = 7f;

    public event Action<Offer> Chosen;

    private readonly Panel[] _panels = new Panel[Slots];
    private readonly Label[] _rarityLabels = new Label[Slots];
    private readonly Label[] _nameLabels = new Label[Slots];
    private readonly Label[] _tagLabels = new Label[Slots];
    private readonly Label[] _textLabels = new Label[Slots];
    private readonly List<Offer> _offers = new();
    private Control _root;
    private int _hover = -1;
    private int _sel;
    private float _pulse;

    public override void _Ready()
    {
        Layer = 10;

        _root = new Control { Position = Vector2.Zero, Size = new Vector2(320f, 180f) };
        AddChild(_root);

        _root.AddChild(new ColorRect { Color = new Color(0f, 0f, 0f, 0.75f), Position = Vector2.Zero, Size = new Vector2(320f, 180f) });

        Label title = UiStyle.MakeLabel("RÁDIO 96.6 INFORMA: ESCOLHA UM ANÚNCIO", UiStyle.Green);
        title.Position = new Vector2(0f, 10f);
        title.Size = new Vector2(320f, 12f);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _root.AddChild(title);

        float startX = (320f - (Slots * SlotW + (Slots - 1) * SlotGap)) * 0.5f;
        for (int i = 0; i < Slots; i++)
        {
            int slot = i;
            var panel = new Panel
            {
                Position = new Vector2(startX + i * (SlotW + SlotGap), 26f),
                Size = new Vector2(SlotW, SlotH),
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            panel.MouseEntered += () => { _hover = slot; _sel = slot; Restyle(); };
            panel.MouseExited += () => { if (_hover == slot) _hover = -1; };
            panel.GuiInput += e =>
            {
                if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) Choose(slot);
            };
            _root.AddChild(panel);
            _panels[i] = panel;

            _rarityLabels[i] = AddLabel(panel, new Vector2(0f, 3f), SlotW, HorizontalAlignment.Center);
            _nameLabels[i] = AddLabel(panel, new Vector2(4f, 16f), SlotW - 8f, HorizontalAlignment.Center);
            _nameLabels[i].AddThemeFontOverride("font", Fonts.Bold);
            _nameLabels[i].AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _tagLabels[i] = AddLabel(panel, new Vector2(0f, 42f), SlotW, HorizontalAlignment.Center);
            _textLabels[i] = AddLabel(panel, new Vector2(5f, 56f), SlotW - 10f, HorizontalAlignment.Left);
            _textLabels[i].AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _textLabels[i].Size = new Vector2(SlotW - 10f, SlotH - 68f);

            var key = UiStyle.MakeLabel($"[{i + 1}]", UiStyle.Hint);
            key.Position = new Vector2(0f, SlotH - 13f);
            key.Size = new Vector2(SlotW, 10f);
            key.HorizontalAlignment = HorizontalAlignment.Center;
            panel.AddChild(key);
        }

        Label hint = UiStyle.MakeLabel("1 2 3, setas + Enter ou clique", UiStyle.GreenDim);
        hint.Position = new Vector2(0f, 156f);
        hint.Size = new Vector2(320f, 12f);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        _root.AddChild(hint);

        Visible = false;
    }

    private static Label AddLabel(Control parent, Vector2 position, float width, HorizontalAlignment align)
    {
        Label label = UiStyle.MakeLabel("", Colors.White);
        label.Position = position;
        label.Size = new Vector2(width, 10f);
        label.HorizontalAlignment = align;
        parent.AddChild(label);
        return label;
    }

    /// <summary>levels: nível atual de cada upgrade, para mostrar "NOVO" ou o próximo nível.</summary>
    public void Open(List<Offer> offers, Dictionary<string, int> levels)
    {
        _offers.Clear();
        _offers.AddRange(offers);
        _sel = 0;
        _hover = -1;

        for (int i = 0; i < Slots; i++)
        {
            if (i >= _offers.Count)
            {
                _panels[i].Visible = false;
                continue;
            }

            Offer offer = _offers[i];
            Color tint = Rarities.Tint[(int)offer.Rarity];
            int current = levels[offer.Def.Id];
            bool infinite = offer.Def.MaxLevel == int.MaxValue;
            string tag = infinite ? "" : current == 0 ? "NOVO" : $"NV {current + 1}";

            _rarityLabels[i].Text = Rarities.Names[(int)offer.Rarity];
            _rarityLabels[i].AddThemeColorOverride("font_color", tint);
            _nameLabels[i].Text = offer.Def.Name;
            _nameLabels[i].AddThemeColorOverride("font_color", Colors.White);
            _tagLabels[i].Text = tag;
            _tagLabels[i].AddThemeColorOverride("font_color", current == 0 && !infinite ? UiStyle.Green : UiStyle.Hint);
            _textLabels[i].Text = UpgradeCatalog.Describe(offer.Def, offer.Rarity, current);
            _textLabels[i].AddThemeColorOverride("font_color", new Color(0.85f, 0.95f, 0.88f));
            _panels[i].Visible = true;
        }

        Restyle();
        Visible = true;
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        _pulse += (float)delta * 5f;
        Restyle();
    }

    private void Restyle()
    {
        for (int i = 0; i < _offers.Count && i < Slots; i++)
        {
            Rarity rarity = _offers[i].Rarity;
            Color tint = Rarities.Tint[(int)rarity];
            bool selected = i == _sel;

            float glow = 0f;
            if (rarity >= Rarity.Epico) glow = (0.5f + 0.5f * Mathf.Sin(_pulse + i)) * (rarity == Rarity.Lendario ? 0.5f : 0.3f);

            Color border = tint.Lerp(Colors.White, selected ? 0.55f : glow);
            Color background = new Color(tint.R * 0.12f, tint.G * 0.12f, tint.B * 0.12f).Lerp(UiStyle.Dark, selected ? 0f : 0.45f);
            if (selected) background = background.Lightened(0.08f);
            int width = selected ? 2 : 1;

            _panels[i].AddThemeStyleboxOverride("panel", UiStyle.Box(background, border, rarity >= Rarity.Raro ? width + 1 : width));
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;

        if (@event.IsActionPressed("ui_left")) { Move(-1); return; }
        if (@event.IsActionPressed("ui_right")) { Move(1); return; }
        if (@event.IsActionPressed("ui_accept")) { GetViewport().SetInputAsHandled(); Choose(_sel); return; }

        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        int slot = key.Keycode switch
        {
            Key.Key1 or Key.Kp1 => 0,
            Key.Key2 or Key.Kp2 => 1,
            Key.Key3 or Key.Kp3 => 2,
            _ => -1,
        };
        if (slot < 0) return;

        GetViewport().SetInputAsHandled();
        Choose(slot);
    }

    private void Move(int direction)
    {
        GetViewport().SetInputAsHandled();
        _sel = Mathf.Clamp(_sel + direction, 0, Math.Max(_offers.Count - 1, 0));
        Restyle();
    }

    private void Choose(int slot)
    {
        if (!Visible || slot < 0 || slot >= _offers.Count) return;
        Offer offer = _offers[slot];
        Visible = false;
        Chosen?.Invoke(offer);
    }
}
