using UnityEngine;

public sealed class CombatCharacter : MonoBehaviour
{
    public enum MovementMotorType
    {
        Transform,
        Rigidbody2D
    }

    [SerializeField] private CharacterMovementSettings _settings = new CharacterMovementSettings();
    [SerializeField] private MonoBehaviour _inputSourceBehaviour;
    [SerializeField] private Transform _movementRoot;
    [SerializeField] private Transform _facingRoot;
    [SerializeField] private MovementMotorType _motorType = MovementMotorType.Transform;
    [SerializeField] private Rigidbody2D _rigidbody;

    private CharacterMovementController _controller;
    private ConfigurableMovementGate _movementGate;

    public IMovementState MovementState => _controller;
    public ConfigurableMovementGate MovementGate => _movementGate;

    private void Awake()
    {
        if (_inputSourceBehaviour == null)
        {
            Debug.LogError($"{nameof(CombatCharacter)} on {name} requires an input source.", this);
            enabled = false;
            return;
        }

        if (_inputSourceBehaviour is not IMovementInputSource inputSource)
        {
            Debug.LogError(
                $"{nameof(CombatCharacter)} on {name} requires {nameof(_inputSourceBehaviour)} to implement {nameof(IMovementInputSource)}.",
                this);
            enabled = false;
            return;
        }

        ICharacterMovementMotor motor = CreateMotor();
        if (motor == null)
        {
            enabled = false;
            return;
        }

        _movementGate = new ConfigurableMovementGate();

        Transform facingRoot = _facingRoot != null ? _facingRoot : transform;
        _controller = new CharacterMovementController(
            inputSource,
            motor,
            _settings,
            _movementGate,
            facingRoot);
    }

    private ICharacterMovementMotor CreateMotor()
    {
        if (_motorType == MovementMotorType.Rigidbody2D)
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }

            if (_rigidbody == null)
            {
                Debug.LogError(
                    $"{nameof(CombatCharacter)} on {name} requires a {nameof(Rigidbody2D)} for {MovementMotorType.Rigidbody2D} movement.",
                    this);
                return null;
            }

            return new Rigidbody2DMovementMotor(_rigidbody);
        }

        Transform movementRoot = _movementRoot != null ? _movementRoot : transform;
        return new TransformMovementMotor(movementRoot);
    }

    private void Update()
    {
        if (_controller == null)
        {
            return;
        }

        _controller.Tick(Time.deltaTime);
    }

    private void OnValidate()
    {
        if (_inputSourceBehaviour != null && _inputSourceBehaviour is not IMovementInputSource)
        {
            Debug.LogWarning(
                $"{nameof(CombatCharacter)} on {name} expects {nameof(_inputSourceBehaviour)} to implement {nameof(IMovementInputSource)}.",
                this);
        }
    }
}
