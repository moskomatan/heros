using System;
using UnityEngine;

public sealed class CharacterCombat : MonoBehaviour, IDamageReceiver, ICombatVitality, IBasicAttackRequester
{
    [SerializeField] private CombatConfiguration _configuration = new CombatConfiguration();
    [SerializeField] private RegisteredCombatant _registeredCombatant;

    private HealthPool _healthPool;
    private DamageReceiver _damageReceiver;
    private ConfigurableMovementGate _movementGate;

    public event Action<int, int> HealthChanged;

    public event Action Died;

    public event Action<DamageResult> DamageApplied;

    public RegisteredCombatant RegisteredCombatant => _registeredCombatant;

    public bool IsAlive => _healthPool != null && _healthPool.IsAlive;

    private void Awake()
    {
        ResolveRegisteredCombatant();
        ConstructRuntimeObjects();
        BindVitalityToCombatant();
        SubscribeRuntimeEvents();
    }

    private void Start()
    {
        ResolveMovementGate();
    }

    private void FixedUpdate()
    {
        _damageReceiver.ResolvePendingBatch();
    }

    private void OnDestroy()
    {
        UnsubscribeRuntimeEvents();
    }

    private void OnValidate()
    {
        _configuration?.Clamp();
        ResolveRegisteredCombatant();
    }

    public DamageResult ReceiveDamage(in DamageRequest request)
    {
        return _damageReceiver.ReceiveDamage(in request);
    }

    // Wired in Task 7 when BasicAttackRuntime is composed.
    public bool TryBasicAttack()
    {
        return false;
    }

    private void ConstructRuntimeObjects()
    {
        _configuration.Clamp();

        _healthPool = new HealthPool(_configuration.Vitality.MaxHealth);
        IDamageMitigation mitigation = new DefenseResolver(
            _configuration.Defense.Defense,
            _configuration.Defense.CriticalDefense);
        _damageReceiver = new DamageReceiver(_healthPool, mitigation);
    }

    private void BindVitalityToCombatant()
    {
        if (_registeredCombatant != null)
        {
            _registeredCombatant.BindVitality(this);
        }
    }

    private void ResolveRegisteredCombatant()
    {
        if (_registeredCombatant == null)
        {
            _registeredCombatant = GetComponent<RegisteredCombatant>();
        }
    }

    private void ResolveMovementGate()
    {
        if (_movementGate != null)
        {
            return;
        }

        CombatCharacter combatCharacter = GetComponent<CombatCharacter>();
        if (combatCharacter != null)
        {
            _movementGate = combatCharacter.MovementGate;
        }
    }

    private void SubscribeRuntimeEvents()
    {
        _healthPool.HealthChanged += OnHealthChanged;
        _healthPool.Died += OnDied;
        _damageReceiver.DamageApplied += OnDamageApplied;
    }

    private void UnsubscribeRuntimeEvents()
    {
        if (_healthPool != null)
        {
            _healthPool.HealthChanged -= OnHealthChanged;
            _healthPool.Died -= OnDied;
        }

        if (_damageReceiver != null)
        {
            _damageReceiver.DamageApplied -= OnDamageApplied;
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        HealthChanged?.Invoke(current, max);
    }

    private void OnDied()
    {
        ResolveMovementGate();
        if (_movementGate != null)
        {
            _movementGate.CanMove = false;
        }

        Died?.Invoke();
    }

    private void OnDamageApplied(DamageResult result)
    {
        DamageApplied?.Invoke(result);
    }
}
