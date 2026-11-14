using System;
using System.Collections.Generic;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>
/// Menu desenhado por código (320x180): páginas com botões, barras de volume e chaves liga/desliga.
/// Teclado (setas, Enter, Esc), controle e mouse funcionam. Usado no menu principal e na pausa.
/// </summary>
public partial class MenuPanel : Control
{
    public enum Kind { Button, Slider, Toggle }

    public sealed class Item
    {
        public Kind Kind;
        public string Text;
        public Func<string> Label;
        public Action OnActivate;
        public Func<float> Get;
        public Action<float> Set;
        public Func<bool> Enabled;
        public Func<string> Hint;
        /// <summary>Som tocado ao mexer na barra, no barramento certo, para a pessoa ouvir o volume.</summary>
        public string PreviewBus;
        public string PreviewSound;

        public bool IsEnabled => Enabled == null || Enabled();
        public string Display => Label != null ? Label() : Text;
    }

    public sealed class Page
    {
        public string Id;
        public readonly List<(string Text, int Size, Color Color)> Title = new();
        public readonly List<Item> Items = new();
        /// <summary>Esc volta daqui; null deixa o Esc passar para quem está por baixo.</summary>
        public Action OnBack;
        public float DimAlpha = 0.78f;
        public int StartIndex = -1;
    }

    private const float RowH = 14f;
    private const float RowGap = 1f;
    private const float RowX = 36f;
    private const float RowW = 248f;
    private const float BarX = 156f;
    private const float BarW = 92f;
    private const float StepSize = 0.05f;

    private readonly Dictionary<string, Page> _pages = new();
    private Page _page;
    private int _sel;
    private bool _dragging;
    private Font _font;
    private AudioStreamPlayer _uiPlayer;
    private AudioStreamPlayer _previewPlayer;

    public static Item Button(string text, Action onActivate, string hint = null, Func<bool> enabled = null)
    {
        return new Item
        {
            Kind = Kind.Button, Text = text, OnActivate = onActivate, Enabled = enabled,
            Hint = hint == null ? null : () => hint,
        };
    }

    public void AddPage(Page page)
    {
        _pages[page.Id] = page;
    }

    public override void _Ready()
    {
        GameSettings.Init();

        Position = Vector2.Zero;
        Size = new Vector2(320f, 180f);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;
        _font = ThemeDB.FallbackFont;

        _uiPlayer = new AudioStreamPlayer { Bus = GameSettings.BusUi };
        AddChild(_uiPlayer);
        _previewPlayer = new AudioStreamPlayer();
        AddChild(_previewPlayer);
    }

    public void Open(string pageId)
    {
        if (!_pages.TryGetValue(pageId, out Page page)) return;

        _page = page;
        _dragging = false;
        _sel = page.StartIndex >= 0 && page.StartIndex < page.Items.Count ? page.StartIndex : FirstEnabled();
        Visible = true;
        QueueRedraw();
    }

    public void Close()
    {
        Visible = false;
        _dragging = false;
    }

    // ---------------------------------------------------------------- Desenho

    public override void _Draw()
    {
        if (_page == null) return;

        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0f, 0f, 0f, _page.DimAlpha));

        float y = 12f;
        foreach ((string text, int size, Color color) in _page.Title)
        {
            DrawCentered(text, y, size, color);
            y += size + 4f;
        }

        float top = ItemsTop();
        for (int i = 0; i < _page.Items.Count; i++)
            DrawItem(_page.Items[i], i, top + i * (RowH + RowGap));

        if (_sel >= 0 && _sel < _page.Items.Count)
        {
            string hint = _page.Items[_sel].Hint?.Invoke();
            if (!string.IsNullOrEmpty(hint)) DrawCentered(hint, 166f, UiStyle.FontSize, UiStyle.Hint);
        }
    }

    private void DrawItem(Item item, int index, float top)
    {
        bool selected = index == _sel;
        bool enabled = item.IsEnabled;
        var rect = new Rect2(RowX, top, RowW, RowH);

        if (selected && enabled)
        {
            DrawRect(rect, UiStyle.SelectBg);
            DrawRect(rect, UiStyle.Green, false, 1f);
        }

        Color color = !enabled ? UiStyle.Disabled : selected ? Colors.White : UiStyle.Green;
        if (item.Kind == Kind.Button)
        {
            DrawCentered(item.Display, top + (RowH - _font.GetHeight(UiStyle.FontSize)) / 2f, UiStyle.FontSize, color, RowX, RowW);
            return;
        }

        float textY = top + (RowH - _font.GetHeight(UiStyle.FontSize)) / 2f + _font.GetAscent(UiStyle.FontSize);
        DrawString(_font, new Vector2(RowX + 8f, textY), item.Display, HorizontalAlignment.Left, -1f, UiStyle.FontSize, color);

        float value = item.Get != null ? item.Get() : 0f;
        if (item.Kind == Kind.Slider)
        {
            float barY = top + RowH / 2f - 2f;
            DrawRect(new Rect2(BarX, barY, BarW, 4f), UiStyle.Dark);
            DrawRect(new Rect2(BarX, barY, BarW, 4f), UiStyle.GreenDim, false, 1f);
            DrawRect(new Rect2(BarX, barY, BarW * Mathf.Clamp(value, 0f, 1f), 4f), selected ? Colors.White : UiStyle.Green);
            DrawString(_font, new Vector2(RowX + RowW - 30f, textY), $"{Mathf.RoundToInt(value * 100f)}%",
                HorizontalAlignment.Right, 24f, UiStyle.FontSize, color);
        }
        else
        {
            DrawString(_font, new Vector2(RowX + RowW - 90f, textY), value > 0.5f ? "LIGADO" : "DESLIGADO",
                HorizontalAlignment.Right, 82f, UiStyle.FontSize, value > 0.5f ? color : UiStyle.Disabled);
        }
    }

    private void DrawCentered(string text, float top, int size, Color color, float left = 0f, float width = -1f)
    {
        if (width < 0f) width = Size.X;
        DrawString(_font, new Vector2(left, top + _font.GetAscent(size)), text, HorizontalAlignment.Center, width, size, color);
    }

    private float ItemsTop()
    {
        float y = 12f;
        foreach ((string _, int size, Color _) in _page.Title) y += size + 4f;
        return y + 8f;
    }

    // ---------------------------------------------------------------- Teclado e controle

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || _page == null) return;

        if (@event.IsActionPressed("ui_down", true)) { Move(1); Consume(); }
        else if (@event.IsActionPressed("ui_up", true)) { Move(-1); Consume(); }
        else if (@event.IsActionPressed("ui_left", true)) { Adjust(-1); Consume(); }
        else if (@event.IsActionPressed("ui_right", true)) { Adjust(1); Consume(); }
        else if (@event.IsActionPressed("ui_accept")) { Activate(); Consume(); }
        else if (@event.IsActionPressed("ui_cancel") && _page.OnBack != null)
        {
            PlayUi("UI/ui_back.wav");
            _page.OnBack();
            Consume();
        }
    }

    private void Consume()
    {
        GetViewport().SetInputAsHandled();
        QueueRedraw();
    }

    private void Move(int direction)
    {
        int count = _page.Items.Count;
        if (count == 0) return;

        for (int step = 0; step < count; step++)
        {
            _sel = (_sel + direction + count) % count;
            if (_page.Items[_sel].IsEnabled) break;
        }
        PlayUi("UI/ui_hover.wav");
    }

    private void Adjust(int direction)
    {
        Item item = CurrentItem();
        if (item == null || !item.IsEnabled) return;

        if (item.Kind == Kind.Slider)
        {
            float before = item.Get();
            float after = Mathf.Clamp(Mathf.Snapped(before + direction * StepSize, StepSize), 0f, 1f);
            if (Mathf.IsEqualApprox(before, after)) return;
            item.Set(after);
            Preview(item);
        }
        else if (item.Kind == Kind.Toggle)
        {
            Toggle(item);
        }
    }

    private void Activate()
    {
        Item item = CurrentItem();
        if (item == null || !item.IsEnabled) return;

        if (item.Kind == Kind.Toggle)
        {
            Toggle(item);
        }
        else if (item.Kind == Kind.Button)
        {
            PlayUi("UI/ui_click.wav");
            item.OnActivate?.Invoke();
        }
    }

    private void Toggle(Item item)
    {
        item.Set(item.Get() > 0.5f ? 0f : 1f);
        PlayUi("UI/ui_click.wav");
    }

    // ---------------------------------------------------------------- Mouse

    public override void _GuiInput(InputEvent @event)
    {
        if (!Visible || _page == null) return;

        if (@event is InputEventMouseMotion motion)
        {
            int row = RowAt(motion.Position);
            if (row >= 0 && row != _sel && _page.Items[row].IsEnabled)
            {
                _sel = row;
                PlayUi("UI/ui_hover.wav");
                QueueRedraw();
            }
            if (_dragging && (motion.ButtonMask & MouseButtonMask.Left) != 0) DragTo(motion.Position.X);
        }
        else if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            if (!button.Pressed)
            {
                _dragging = false;
                return;
            }

            int row = RowAt(button.Position);
            if (row < 0 || !_page.Items[row].IsEnabled) return;

            _sel = row;
            Item item = _page.Items[row];
            if (item.Kind == Kind.Slider)
            {
                _dragging = button.Position.X >= BarX - 6f;
                if (_dragging) DragTo(button.Position.X);
            }
            else
            {
                Activate();
            }
            AcceptEvent();
            QueueRedraw();
        }
    }

    private void DragTo(float x)
    {
        Item item = CurrentItem();
        if (item == null || item.Kind != Kind.Slider) return;

        float before = item.Get();
        float after = Mathf.Clamp(Mathf.Snapped((x - BarX) / BarW, StepSize), 0f, 1f);
        if (Mathf.IsEqualApprox(before, after)) return;
        item.Set(after);
        Preview(item);
        QueueRedraw();
    }

    private int RowAt(Vector2 position)
    {
        if (position.X < RowX || position.X > RowX + RowW) return -1;

        float top = ItemsTop();
        for (int i = 0; i < _page.Items.Count; i++)
        {
            float rowTop = top + i * (RowH + RowGap);
            if (position.Y >= rowTop && position.Y < rowTop + RowH) return i;
        }
        return -1;
    }

    // ---------------------------------------------------------------- Apoio

    private Item CurrentItem()
    {
        return _page != null && _sel >= 0 && _sel < _page.Items.Count ? _page.Items[_sel] : null;
    }

    private int FirstEnabled()
    {
        for (int i = 0; i < _page.Items.Count; i++)
        {
            if (_page.Items[i].IsEnabled) return i;
        }
        return 0;
    }

    private void PlayUi(string relativePath)
    {
        AudioStream stream = Assets.Stream(relativePath);
        if (stream == null) return;
        _uiPlayer.Stream = stream;
        _uiPlayer.Play();
    }

    private void Preview(Item item)
    {
        if (item.PreviewSound == null) return;
        AudioStream stream = Assets.Stream(item.PreviewSound);
        if (stream == null) return;

        _previewPlayer.Bus = item.PreviewBus ?? GameSettings.BusUi;
        _previewPlayer.Stream = stream;
        _previewPlayer.Play();
    }
}
