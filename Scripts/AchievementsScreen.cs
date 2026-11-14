using Godot;

namespace DeadFrequencyHorda;

/// <summary>Lista de conquistas com rolagem (setas, roda do mouse).</summary>
public partial class AchievementsScreen : PanelScreen
{
    private const float ListTop = 34f;
    private const float RowH = 25f;
    private const int Visible_ = 5;
    private const float ListX = 28f;
    private const float ListW = 264f;

    private int _first;
    private int _sel;

    protected override void OnOpen()
    {
        _sel = 0;
        _first = 0;
    }

    public override void _Draw()
    {
        if (!Visible) return;

        Backdrop(new Rect2(ListX - 10f, 5f, ListW + 20f, 170f));

        int total = Achievements.All.Length;
        int done = Achievements.UnlockedCount;
        Text(ListX, 9f, "CONQUISTAS", UiStyle.Green, 12, bold: true);
        Text(ListX, 10f, $"{done}/{total}", Gold, UiStyle.FontSize, HorizontalAlignment.Right, ListW);

        DrawRect(new Rect2(ListX, 25f, ListW, 3f), UiStyle.Dark);
        DrawRect(new Rect2(ListX, 25f, ListW * done / total, 3f), Gold);

        for (int i = 0; i < Visible_; i++)
        {
            int index = _first + i;
            if (index >= total) break;
            DrawRow(Achievements.All[index], index == _sel, ListTop + i * RowH);
        }

        // Barra de rolagem.
        if (total > Visible_)
        {
            float trackH = Visible_ * RowH - 3f;
            float thumbH = Mathf.Max(8f, trackH * Visible_ / total);
            float thumbY = ListTop + (trackH - thumbH) * _first / (total - Visible_);
            DrawRect(new Rect2(ListX + ListW + 3f, ListTop, 2f, trackH), UiStyle.Dark);
            DrawRect(new Rect2(ListX + ListW + 3f, thumbY, 2f, thumbH), UiStyle.Green);
        }

        Text(ListX, 162f, "Setas ou roda: rolar", UiStyle.Hint);
        Text(ListX, 162f, "Esc: voltar", UiStyle.Hint, UiStyle.FontSize, HorizontalAlignment.Right, ListW);
    }

    private void DrawRow(AchievementDef def, bool selected, float top)
    {
        bool unlocked = Achievements.IsUnlocked(def.Id);
        var rect = new Rect2(ListX, top, ListW, RowH - 2f);
        DrawRect(rect, selected ? UiStyle.SelectBg : new Color(0.03f, 0.08f, 0.05f, 0.9f));
        DrawRect(rect, unlocked ? Gold : UiStyle.GreenDim, false, 1f);

        // Selo: dourado quando desbloqueada.
        var seal = new Rect2(ListX + 4f, top + 4f, 15f, 15f);
        DrawRect(seal, unlocked ? new Color(0.45f, 0.32f, 0.05f) : new Color(0.08f, 0.1f, 0.09f));
        DrawRect(seal, unlocked ? Gold : UiStyle.Disabled, false, 1f);
        Text(seal.Position.X, seal.Position.Y + 1f, unlocked ? "★" : "?", unlocked ? Gold : UiStyle.Disabled, UiStyle.FontSize,
            HorizontalAlignment.Center, seal.Size.X);

        bool hide = def.Secret && !unlocked;
        Color nameColor = unlocked ? Colors.White : UiStyle.Disabled;
        Text(ListX + 24f, top + 1f, hide ? "Conquista secreta" : def.Name, nameColor, UiStyle.FontSize, bold: true);
        Text(ListX + 24f, top + 12f, hide ? "Continue jogando para descobrir." : def.Description,
            unlocked ? UiStyle.Hint : UiStyle.Disabled);
        if (def.Reward > 0)
        {
            Text(ListX, top + 1f, $"+{def.Reward}", unlocked ? Gold : UiStyle.Disabled, UiStyle.FontSize,
                HorizontalAlignment.Right, ListW - 4f);
        }
    }

    protected override bool HandleKey(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_down")) return Move(1);
        if (@event.IsActionPressed("ui_up")) return Move(-1);
        if (@event.IsActionPressed("ui_page_down")) return Move(Visible_);
        if (@event.IsActionPressed("ui_page_up")) return Move(-Visible_);
        return false;
    }

    protected override bool HandlePointer(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true } button)
        {
            if (button.ButtonIndex == MouseButton.WheelDown) return Move(1);
            if (button.ButtonIndex == MouseButton.WheelUp) return Move(-1);
            if (button.ButtonIndex == MouseButton.Left && button.Position.Y > 158f)
            {
                Close();
                return true;
            }
        }
        return false;
    }

    private bool Move(int step)
    {
        int total = Achievements.All.Length;
        int before = _sel;
        _sel = Mathf.Clamp(_sel + step, 0, total - 1);
        if (_sel < _first) _first = _sel;
        if (_sel >= _first + Visible_) _first = _sel - Visible_ + 1;
        if (before != _sel) PlayUi("UI/ui_hover.wav");
        return true;
    }
}
