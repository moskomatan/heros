using UnityEngine;

public sealed class CharacterMovementController : IMovementState
{
    private readonly IMovementInputSource _input;
    private readonly ICharacterMovementMotor _motor;
    private readonly CharacterMovementSettings _settings;
    private readonly IMovementGate _gate;
    private readonly Transform _facingRoot;

    private Vector2 _direction;
    private Vector2 _lastNonZeroDirection = Vector2.right;
    private Vector2 _velocity;

    public CharacterMovementController(
        IMovementInputSource input,
        ICharacterMovementMotor motor,
        CharacterMovementSettings settings,
        IMovementGate gate,
        Transform facingRoot)
    {
        _input = input;
        _motor = motor;
        _settings = settings;
        _gate = gate;
        _facingRoot = facingRoot;
        ApplyFacingScale();
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
            ApplyFacingScale();
        }

        if (IsMoving)
        {
            _motor.Move(_velocity, deltaTime);
        }
    }

    private void ApplyFacingScale()
    {
        if (_facingRoot == null)
        {
            return;
        }

        float facingX = _lastNonZeroDirection.x;
        if (Mathf.Abs(facingX) <= 0.0001f)
        {
            return;
        }

        float sign = Mathf.Sign(facingX);
        Vector3 scale = _facingRoot.localScale;
        float absX = Mathf.Abs(scale.x);
        if (absX <= 0.0001f)
        {
            absX = 1f;
        }

        float newX = absX * sign;
        if (Mathf.Approximately(scale.x, newX))
        {
            return;
        }

        _facingRoot.localScale = new Vector3(newX, scale.y, scale.z);
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
