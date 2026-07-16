using UnityEngine;

public sealed class RegisteredCombatant : MonoBehaviour
{
    [SerializeField] private CombatCharacter _character;
    [SerializeField] private TeamMember _teamMember;
    [SerializeField] private Transform _targetTransform;

    private ICombatantRegistry _registry;
    private bool _isInitialized;

    public CombatCharacter Character => _character;
    public ITeamMember TeamMember => _teamMember;
    public Transform TargetTransform => _targetTransform != null ? _targetTransform : transform;

    public bool IsTargetable =>
        isActiveAndEnabled &&
        gameObject.activeInHierarchy &&
        _character != null &&
        _teamMember != null;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        if (_isInitialized && _registry != null)
        {
            _registry.Register(this);
        }
    }

    private void OnDisable()
    {
        if (_registry != null)
        {
            _registry.Unregister(this);
        }
    }

    private void OnDestroy()
    {
        if (_registry != null)
        {
            _registry.Unregister(this);
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
        ValidateReferences();
    }

    public void Initialize(ICombatantRegistry registry)
    {
        if (registry == null)
        {
            Debug.LogError($"{nameof(RegisteredCombatant)} on {name} received a null registry.", this);
            return;
        }

        if (_isInitialized)
        {
            if (_registry == registry)
            {
                if (isActiveAndEnabled)
                {
                    _registry.Register(this);
                }

                return;
            }

            Debug.LogError(
                $"{nameof(RegisteredCombatant)} on {name} was already initialized with a different registry.",
                this);
            return;
        }

        _registry = registry;
        _isInitialized = true;

        if (isActiveAndEnabled)
        {
            _registry.Register(this);
        }
    }

    private void ResolveReferences()
    {
        if (_character == null)
        {
            _character = GetComponent<CombatCharacter>();
        }

        if (_teamMember == null)
        {
            _teamMember = GetComponent<TeamMember>();
        }
    }

    private void ValidateReferences()
    {
        if (_character == null)
        {
            Debug.LogError(
                $"{nameof(RegisteredCombatant)} on {name} requires a {nameof(CombatCharacter)} reference.",
                this);
        }

        if (_teamMember == null)
        {
            Debug.LogError(
                $"{nameof(RegisteredCombatant)} on {name} requires a {nameof(TeamMember)} reference.",
                this);
        }
    }
}
