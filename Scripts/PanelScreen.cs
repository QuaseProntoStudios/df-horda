using System;
using Godot;

namespace DeadFrequencyHorda;

/// <summary>Base das telas cheias do menu (Conquistas, Oficina): painel escuro, texto com contorno e Esc para voltar.</summary>
public abstract partial class PanelScreen : Control
{
    protected static readonly Color Gold = new(1f, 0.78f, 0.25f);

    private AudioStreamPlayer _uiPlayer;

    /// <summary>Disparado quando a tela fecha, para o menu reabrir a página anterior.</summary>
    public event Action Closed;

    public override void _Ready()
    {
        Fonts.Init();
        Position = Vector2.Zero;
        Size = new Vector2(320f, 180f);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;
        Visible = false;

        _uiPlayer = new AudioStreamPlayer { Bus = GameSettings.BusUi };
        AddChild(_uiPlayer);
    }

    public void Open()
    {
        Visible = true;
        OnOpen();
        QueueRedraw();
    }

    public void Close()
    {
        if (!Visible) return;
        Visible = false;
        PlayUi("UI/ui_back.wav");
        Closed?.Invoke();
    }

    protected virtual void OnOpen() { }

    protected void PlayUi(string relativePath)
    {
        AudioStream stream = Assets.Stream(relativePath);
        if (stream == null) return;
        _uiPlayer.Stream = stream;
        _uiPlayer.Play();
    }

    protected void Backdrop(Rect2 panel)
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0f, 0f, 0f, 0.55f));
        DrawRect(panel, new Color(0.01f, 0.04f, 0.02f, 0.88f));
        DrawRect(panel, UiStyle.GreenDim, false, 1f);
    }

    /// <summary>Texto com contorno preto. top é a borda de cima da linha de texto.</summary>
    protected void Text(float x, float top, string text, Color color, int size = Fonts.BodySize,
        HorizontalAlignment align = HorizontalAlignment.Left, float width = -1f, bool bold = false)
    {
        Font font = bold ? Fonts.Bold : Fonts.Regular;
        var position = new Vector2(Mathf.Round(x), Mathf.Round(top + font.GetAscent(size)));
        DrawStringOutline(font, position, text, align, width, size, 3, Colors.Black);
        DrawString(font, position, text, align, width, size, color);
    }

    protected float TextWidth(string text, int size = Fonts.BodySize, bool bold = false)
    {
        return (bold ? Fonts.Bold : Fonts.Regular).GetStringSize(text, HorizontalAlignment.Left, -1f, size).X;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;

        if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
        else if (HandleKey(@event))
        {
            GetViewport().SetInputAsHandled();
            QueueRedraw();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Visible) return;
        if (HandlePointer(@event))
        {
            AcceptEvent();
            QueueRedraw();
        }
    }

    protected virtual bool HandleKey(InputEvent @event) { return false; }
    protected virtual bool HandlePointer(InputEvent @event) { return false; }
}
