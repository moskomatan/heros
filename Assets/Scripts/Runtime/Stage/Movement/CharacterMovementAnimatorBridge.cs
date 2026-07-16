using UnityEngine;

public sealed class CharacterMovementAnimatorBridge : MonoBehaviour
{
    [SerializeField] private CombatCharacter _character;
    [SerializeField] private Animator _animator;
    [SerializeField] private string _isMovingParameter = "IsMoving";
    [SerializeField] private string _moveXParameter = "MoveX";
    [SerializeField] private string _moveYParameter = "MoveY";
    [SerializeField] private string _speedParameter = "Speed";

    private int _isMovingHash;
    private int _moveXHash;
    private int _moveYHash;
    private int _speedHash;

    private void Awake()
    {
        if (_character == null)
        {
            _character = GetComponent<CombatCharacter>();
        }

        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        _isMovingHash = Animator.StringToHash(_isMovingParameter);
        _moveXHash = Animator.StringToHash(_moveXParameter);
        _moveYHash = Animator.StringToHash(_moveYParameter);
        _speedHash = Animator.StringToHash(_speedParameter);
    }

    private void Update()
    {
        if (_character == null || _animator == null)
        {
            return;
        }

        IMovementState movementState = _character.MovementState;
        if (movementState == null)
        {
            return;
        }

        _animator.SetBool(_isMovingHash, movementState.IsMoving);
        _animator.SetFloat(_moveXHash, movementState.Direction.x);
        _animator.SetFloat(_moveYHash, movementState.Direction.y);
        _animator.SetFloat(_speedHash, movementState.NormalizedSpeed);
    }
}
