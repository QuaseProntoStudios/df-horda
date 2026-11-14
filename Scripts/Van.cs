using Godot;

namespace DeadFrequencyHorda;

/// <summary>
/// A van do jogador. Movimento de carro visto de cima: acelera para onde aponta,
/// tem inércia e derrapa (a aderência lateral é limitada). Sem física do Godot:
/// o Game chama Tick() a cada quadro.
/// </summary>
public partial class Van : Node2D
{
    public const float Radius = 9f;

    public float MaxSpeed = 105f;
    public float Acceleration = 170f;
    public float ReverseFactor = 0.55f;
    public float Drag = 1.1f;
    public float LateralGrip = 4.5f;
    public float TurnRate = 3.4f;

    /// <summary>Direção para onde a frente aponta, em radianos (0 = direita).</summary>
    public float Heading = -Mathf.Pi / 2f;
    public Vector2 Velocity = Vector2.Zero;

    private Sprite2D _sprite;
    private float _flashTime;

    public float Speed => Velocity.Length();
    public Vector2 Forward => Vector2.Right.Rotated(Heading);

    public override void _Ready()
    {
        _sprite = new Sprite2D
        {
            Texture = Assets.Texture("van"),
            ZIndex = 1,
        };
        AddChild(_sprite);
        UpdateSprite();
    }

    public void Tick(float dt)
    {
        float throttle = 0f;
        float steer = 0f;
        if (Input.IsPhysicalKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) throttle += 1f;
        if (Input.IsPhysicalKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) throttle -= 1f;
        if (Input.IsPhysicalKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) steer += 1f;
        if (Input.IsPhysicalKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) steer -= 1f;

        // Controle: gatilhos aceleram/freiam, analógico esquerdo vira.
        throttle += Input.GetJoyAxis(0, JoyAxis.TriggerRight) - Input.GetJoyAxis(0, JoyAxis.TriggerLeft);
        float stick = Input.GetJoyAxis(0, JoyAxis.LeftX);
        if (Mathf.Abs(stick) > 0.2f) steer += stick;
        throttle = Mathf.Clamp(throttle, -1f, 1f);
        steer = Mathf.Clamp(steer, -1f, 1f);

        Vector2 forward = Forward;
        float forwardSpeed = Velocity.Dot(forward);

        // Só vira andando; de ré o volante inverte, como num carro.
        float steerScale = Mathf.Clamp(forwardSpeed / 40f, -1f, 1f);
        Heading += steer * TurnRate * steerScale * dt;
        forward = Forward;

        float power = throttle >= 0f ? Acceleration : Acceleration * ReverseFactor;
        Velocity += forward * (throttle * power * dt);

        // Separa a velocidade em "para frente" e "de lado": o lado morre rápido (pneu),
        // a frente morre devagar (arrasto). Aderência baixa = derrapagem.
        forwardSpeed = Velocity.Dot(forward);
        Vector2 lateral = Velocity - forward * forwardSpeed;
        forwardSpeed *= Mathf.Exp(-Drag * dt);
        lateral *= Mathf.Exp(-LateralGrip * dt);
        Velocity = forward * forwardSpeed + lateral;

        float speed = Velocity.Length();
        if (speed > MaxSpeed)
            Velocity *= MaxSpeed / speed;

        Position += Velocity * dt;

        if (_flashTime > 0f) _flashTime -= dt;
        UpdateSprite();
    }

    public void Flash()
    {
        _flashTime = 0.15f;
    }

    private void UpdateSprite()
    {
        // O sprite foi desenhado com a frente para cima.
        _sprite.Rotation = Heading + Mathf.Pi / 2f;
        _sprite.Modulate = _flashTime > 0f ? new Color(1f, 0.35f, 0.35f) : Colors.White;
    }
}
