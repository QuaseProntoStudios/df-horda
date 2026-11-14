using Godot;

namespace DeadFrequencyHorda;

/// <summary>Anel que se abre e some: o visual do pulso do Carro de Som.</summary>
public partial class PulseFx : Node2D
{
    private const float Duration = 0.35f;

    private float _maxRadius;
    private float _age;

    public static PulseFx Create(Vector2 position, float radius)
    {
        return new PulseFx { Position = position, _maxRadius = radius, ZIndex = 3 };
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= Duration)
        {
            QueueFree();
            return;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float t = _age / Duration;
        float radius = Mathf.Lerp(4f, _maxRadius, t);
        var color = new Color(0.2f, 1f, 0.4f, 1f - t);
        DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 32, color, 1f);
        if (radius > 6f)
            DrawArc(Vector2.Zero, radius - 4f, 0f, Mathf.Tau, 32, new Color(color.R, color.G, color.B, color.A * 0.5f), 1f);
    }
}
