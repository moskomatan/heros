using UnityEngine;

public sealed class CharacterMovementController : IMovementState
{
    private readonly IMovementInputSource _input;
    private readonly ICharacterMovementMotor _motor;
    private readonly CharacterMovementSettings _settings;
    private readonly IMovementGate _gate;

    private Vector2 _direction;
    private Vector2 _lastNonZeroDirection = Vector2.right;
    private Vector2 _velocity;

    public CharacterMovementController(
        IMovementInputSource input,
        ICharacterMovementMotor motor,
        CharacterMovementSettings settings,
        IMovementGate gate)
    {
        _input = input;
        _motor = motor;
        _settings = settings;
        _gate = gate;
    }

    public Vector2 Direction => _direction;
    public Vector2 LastNonZeroDirection => _lastNonZeroDirection;
    public Vector2 Velocity => _velocity;
    public bool IsMoving => _velocity.sqrMagnitude > 0.0001f;
    public float NormalizedSpeed
    {
        get
        {
            if (_settings.MoveSpeed <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(_velocity.magnitude / _settings.MoveSpeed);
        }
    }

    public void Tick(float deltaTime)
    {
        Vector2 rawDirection = _gate.CanMove ? _input.GetDirection() : Vector2.zero;
        _direction = ProcessDirection(rawDirection);

        float speed = _settings.MoveSpeed * _gate.SpeedMultiplier;
        _velocity = _direction * speed;

        if (_direction.sqrMagnitude > 0.0001f)
        {
            _lastNonZeroDirection = _direction;
        }

        if (IsMoving)
        {
            _motor.Move(_velocity, deltaTime);
        }
    }

    private Vector2 ProcessDirection(Vector2 rawDirection)
    {
        if (rawDirection.sqrMagnitude <= _settings.DeadZone * _settings.DeadZone)
        {
            return Vector2.zero;
        }

        Vector2 direction = rawDirection;

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }
        else if (_settings.NormalizeDiagonals && direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();
        }

        return direction;
    }
}
