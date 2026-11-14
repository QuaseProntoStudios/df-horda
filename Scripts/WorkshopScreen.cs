using Godot;

namespace DeadFrequencyHorda;

/// <summary>A Oficina da 96.6: gasta as fichas ganhas nas partidas em upgrades permanentes.</summary>
public partial class WorkshopScreen : PanelScreen
{
    private const float ListX = 28f;
    private const float ListW = 264f;
    private const float RowTop = 30f;
    private const float RowH = 14f;

    private int _sel;
    private string _message = "";
    private float _messageTime;

    protected override void OnOpen()
    {
        _sel = 0;
        _message = "";
    }

    public override void _Process(double delta)
    {
        if (!Visible || _messageTime <= 0f) return;
        _messageTime -= (float)delta;
        if (_messageTime <= 0f)
        {
            _message = "";
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!Visible) return;

        Backdrop(new Rect2(ListX - 10f, 5f, ListW + 20f, 170f));
        Text(ListX, 9f, "OFICINA DA 96.6", UiStyle.Green, 12, bold: true);
        Text(ListX, 10f, $"FICHAS: {MetaProgress.Fichas}", Gold, UiStyle.FontSize, HorizontalAlignment.Right, ListW, bold: true);
        Text(ListX, 21f, "Fichas vêm de cada partida. Upgrades valem para todas as próximas.", UiStyle.Hint);

        for (int i = 0; i < MetaProgress.All.Length; i++)
            DrawRow(MetaProgress.All[i], i == _sel, RowTop + i * (RowH + 1f));

        MetaDef def = MetaProgress.All[_sel];
        int level = MetaProgress.Level(def.Id);
        string now = level > 0 ? $"Agora: {def.Describe(level)}." : "Ainda não comprado.";
        string next = level < def.MaxLevel ? $" Próximo: {def.Describe(level + 1)}." : " Nível máximo!";
        float y = RowTop + MetaProgress.All.Length * (RowH + 1f) + 3f;
        Text(ListX, y, now + next, Colors.White, UiStyle.FontSize, HorizontalAlignment.Left, ListW);

        string footer = _message != "" ? _message : "Enter: comprar";
        Text(ListX, 162f, footer, _message != "" ? Gold : UiStyle.Hint);
        Text(ListX, 162f, "Esc: voltar", UiStyle.Hint, UiStyle.FontSize, HorizontalAlignment.Right, ListW);
    }

    private void DrawRow(MetaDef def, bool selected, float top)
    {
        int level = MetaProgress.Level(def.Id);
        bool maxed = level >= def.MaxLevel;
        bool canBuy = MetaProgress.CanBuy(def);

        var rect = new Rect2(ListX, top, ListW, RowH);
        if (selected)
        {
            DrawRect(rect, UiStyle.SelectBg);
            DrawRect(rect, UiStyle.Green, false, 1f);
        }

        Color nameColor = selected ? Colors.White : UiStyle.Green;
        Text(ListX + 6f, top + 1f, def.Name, nameColor);

        // Pontinhos de nível.
        float dotX = ListX + 124f;
        for (int i = 0; i < def.MaxLevel; i++)
        {
            Color dot = i < level ? Gold : new Color(0.12f, 0.16f, 0.13f);
            DrawRect(new Rect2(dotX + i * 5f, top + 5f, 4f, 4f), dot);
        }

        string cost = maxed ? "MÁX" : $"{def.CostAt(level)}";
        Color costColor = maxed ? UiStyle.Hint : canBuy ? Gold : UiStyle.Disabled;
        Text(ListX, top + 1f, cost, costColor, UiStyle.FontSize, HorizontalAlignment.Right, ListW - 6f, bold: true);
    }

    protected override bool HandleKey(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_down")) return Select(_sel + 1);
        if (@event.IsActionPressed("ui_up")) return Select(_sel - 1);
        if (@event.IsActionPressed("ui_accept")) { TryBuy(); return true; }
        return false;
    }

    protected override bool HandlePointer(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            int row = RowAt(motion.Position);
            return row >= 0 && row != _sel && Select(row);
        }

        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } button)
        {
            if (button.Position.Y > 158f) { Close(); return true; }

            int row = RowAt(button.Position);
            if (row < 0) return false;
            _sel = row;
            TryBuy();
            return true;
        }
        return false;
    }

    private int RowAt(Vector2 position)
    {
        if (position.X < ListX || position.X > ListX + ListW) return -1;
        int row = Mathf.FloorToInt((position.Y - RowTop) / (RowH + 1f));
        return row >= 0 && row < MetaProgress.All.Length ? row : -1;
    }

    private bool Select(int index)
    {
        int clamped = Mathf.Clamp(index, 0, MetaProgress.All.Length - 1);
        if (clamped != _sel) PlayUi("UI/ui_hover.wav");
        _sel = clamped;
        return true;
    }

    private void TryBuy()
    {
        MetaDef def = MetaProgress.All[_sel];
        int level = MetaProgress.Level(def.Id);
        if (level >= def.MaxLevel)
        {
            Say("Esse já está no máximo.");
        }
        else if (MetaProgress.Buy(def))
        {
            PlayUi("SFX/buy.wav");
            Say($"{def.Name} subiu para o nível {level + 1}!");
        }
        else
        {
            Say($"Faltam {def.CostAt(level) - MetaProgress.Fichas} fichas.");
        }
    }

    private void Say(string message)
    {
        _message = message;
        _messageTime = 2.5f;
    }
}
