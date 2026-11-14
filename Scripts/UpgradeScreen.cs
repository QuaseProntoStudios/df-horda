using System;
using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>Tela de subir de nível: três anúncios da rádio, escolha com clique ou teclas 1, 2, 3.</summary>
public partial class UpgradeScreen : CanvasLayer
{
    private const int Slots = 3;

    public event Action<UpgradeDef> Chosen;

    private readonly Button[] _buttons = new Button[Slots];
    private readonly List<UpgradeDef> _offers = new();
    private Control _root;

    public override void _Ready()
    {
        Layer = 10;

        _root = new Control { Position = Vector2.Zero, Size = new Vector2(320f, 180f) };
        AddChild(_root);

        _root.AddChild(new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.7f),
            Position = Vector2.Zero,
            Size = new Vector2(320f, 180f),
        });

        Label title = UiStyle.MakeLabel("RÁDIO 96.6 INFORMA: ESCOLHA UM ANÚNCIO", UiStyle.Green);
        title.Position = new Vector2(0f, 12f);
        title.Size = new Vector2(320f, 12f);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _root.AddChild(title);

        StyleBoxFlat normal = UiStyle.Box(UiStyle.Dark, UiStyle.GreenDim);
        StyleBoxFlat hover = UiStyle.Box(new Color(0.05f, 0.18f, 0.08f), UiStyle.Green);

        for (int i = 0; i < Slots; i++)
        {
            var button = new Button
            {
                Position = new Vector2(20f, 32f + i * 46f),
                Size = new Vector2(280f, 40f),
                Alignment = HorizontalAlignment.Left,
                FocusMode = Control.FocusModeEnum.None,
            };
            button.AddThemeFontSizeOverride("font_size", UiStyle.FontSize);
            button.AddThemeColorOverride("font_color", UiStyle.Green);
            button.AddThemeColorOverride("font_hover_color", Colors.White);
            button.AddThemeColorOverride("font_pressed_color", Colors.White);
            button.AddThemeStyleboxOverride("normal", normal);
            button.AddThemeStyleboxOverride("hover", hover);
            button.AddThemeStyleboxOverride("pressed", hover);

            int slot = i;
            button.Pressed += () => Choose(slot);
            _root.AddChild(button);
            _buttons[i] = button;
        }

        Visible = false;
    }

    /// <summary>levels: nível atual de cada upgrade, para mostrar o texto do próximo nível.</summary>
    public void Open(List<UpgradeDef> offers, Dictionary<string, int> levels)
    {
        _offers.Clear();
        _offers.AddRange(offers);

        for (int i = 0; i < Slots; i++)
        {
            if (i >= _offers.Count)
            {
                _buttons[i].Visible = false;
                continue;
            }

            UpgradeDef def = _offers[i];
            int current = levels[def.Id];
            string tag = current == 0 ? "NOVO" : $"NV {current + 1}";
            _buttons[i].Text = $"[{i + 1}] {def.Name}  ({tag})\n{def.Levels[current]}";
            _buttons[i].Visible = true;
        }

        Visible = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;
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

    private void Choose(int slot)
    {
        if (!Visible || slot >= _offers.Count) return;
        UpgradeDef def = _offers[slot];
        Visible = false;
        Chosen?.Invoke(def);
    }
}
