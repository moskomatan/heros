using UnityEngine;

public sealed class BotTargetController : MonoBehaviour
{
    [SerializeField] private RegisteredCombatant _registeredCombatant;
    [SerializeField] private BotMovementInputSource _movementInputSource;
    [SerializeField] private float _reevaluateInterval = 0.25f;

    private ICombatantRegistry _registry;
    private ITargetSelector _targetSelector;
    private ITeamRelationshipService _relationshipService;
    private RegisteredCombatant _currentTarget;
    private float _reevaluateTimer;
    private bool _isInitialized;
    private bool _hasLoggedConfigErrors;

    private void Awake()
    {
        ResolveReferences();
        ValidateInterval();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ValidateInterval();
    }

    private void OnEnable()
    {
        _reevaluateTimer = 0f;
    }

    private void OnDisable()
    {
        ClearTarget();
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (!HasValidConfig())
        {
            return;
        }

        if (_currentTarget != null && !IsValidTarget(_currentTarget))
        {
            ClearTarget();
        }

        _reevaluateTimer -= Time.deltaTime;
        if (_reevaluateTimer > 0f)
        {
            return;
        }

        _reevaluateTimer = _reevaluateInterval;
        EvaluateTarget();
    }

    public void Initialize(
        ICombatantRegistry registry,
        ITargetSelector selector,
        ITeamRelationshipService relationshipService)
    {
        if (registry == null)
        {
            LogConfigErrorOnce($"{nameof(BotTargetController)} on {name} received a null registry.");
            return;
        }

        if (selector == null)
        {
            LogConfigErrorOnce($"{nameof(BotTargetController)} on {name} received a null target selector.");
            return;
        }

        if (relationshipService == null)
        {
            LogConfigErrorOnce($"{nameof(BotTargetController)} on {name} received a null relationship service.");
            return;
        }

        if (_isInitialized)
        {
            if (_registry == registry &&
                _targetSelector == selector &&
                _relationshipService == relationshipService)
            {
                return;
            }

            LogConfigErrorOnce(
                $"{nameof(BotTargetController)} on {name} was already initialized with different dependencies.");
            return;
        }

        _registry = registry;
        _targetSelector = selector;
        _relationshipService = relationshipService;
        _isInitialized = true;
    }

    private void EvaluateTarget()
    {
        RegisteredCombatant selected = _targetSelector.SelectTarget(
            _registeredCombatant,
            _registry.Combatants,
            _relationshipService);

        _currentTarget = selected;
        _movementInputSource.Target = selected != null ? selected.TargetTransform : null;
    }

    private bool IsValidTarget(RegisteredCombatant target)
    {
        if (target == null || !target.IsTargetable || _registeredCombatant == null)
        {
            return false;
        }

        return _relationshipService.AreEnemies(_registeredCombatant.TeamMember, target.TeamMember);
    }

    private void ClearTarget()
    {
        _currentTarget = null;

        if (_movementInputSource != null)
        {
            _movementInputSource.Target = null;
        }
    }

    private void ResolveReferences()
    {
        if (_registeredCombatant == null)
        {
            _registeredCombatant = GetComponent<RegisteredCombatant>();
        }

        if (_movementInputSource == null)
        {
            _movementInputSource = GetComponent<BotMovementInputSource>();
        }
    }

    private void ValidateInterval()
    {
        if (_reevaluateInterval <= 0f)
        {
            _reevaluateInterval = 0.25f;
        }
    }

    private bool HasValidConfig()
    {
        if (_registeredCombatant == null)
        {
            LogConfigErrorOnce(
                $"{nameof(BotTargetController)} on {name} requires a {nameof(RegisteredCombatant)} reference.");
            return false;
        }

        if (_movementInputSource == null)
        {
            LogConfigErrorOnce(
                $"{nameof(BotTargetController)} on {name} requires a {nameof(BotMovementInputSource)} reference.");
            return false;
        }

        if (_registry == null)
        {
            LogConfigErrorOnce($"{nameof(BotTargetController)} on {name} requires initialization with a registry.");
            return false;
        }

        if (_targetSelector == null)
        {
            LogConfigErrorOnce($"{nameof(BotTargetController)} on {name} requires initialization with a target selector.");
            return false;
        }

        if (_relationshipService == null)
        {
            LogConfigErrorOnce(
                $"{nameof(BotTargetController)} on {name} requires initialization with a relationship service.");
            return false;
        }

        return true;
    }

    private void LogConfigErrorOnce(string message)
    {
        if (_hasLoggedConfigErrors)
        {
            return;
        }

        _hasLoggedConfigErrors = true;
        Debug.LogError(message, this);
    }
}
