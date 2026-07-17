using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterCombat : MonoBehaviour, IDamageReceiver, ICombatVitality, IBasicAttackRequester
{
    [SerializeField] private CombatConfiguration _configuration = new CombatConfiguration();
    [SerializeField] private RegisteredCombatant _registeredCombatant;
    [SerializeField] private Collider2D _basicAttackHitbox;

    private HealthPool _healthPool;
    private DamageReceiver _damageReceiver;
    private ConfigurableMovementGate _movementGate;
    private readonly Dictionary<Collider2D, IDamageReceiver> _hitReceiverCache = new();
    private readonly List<Collider2D> _overlapBuffer = new(8);
    private ContactFilter2D _overlapFilter;

    public event Action<int, int> HealthChanged;

    public event Action Died;

    public event Action<DamageResult> DamageApplied;

    /// <summary>
    /// Raised when the basic attack hitbox contacts a resolvable damage receiver.
    /// BasicAttackRuntime (Task 7) subscribes for team/damage handling.
    /// </summary>
    public event Action<IDamageReceiver, Vector3> HitCandidateDetected;

    public RegisteredCombatant RegisteredCombatant => _registeredCombatant;

    public Collider2D BasicAttackHitbox => _basicAttackHitbox;

    public bool IsAlive => _healthPool != null && _healthPool.IsAlive;

    private void Awake()
    {
        ResolveRegisteredCombatant();
        ConstructRuntimeObjects();
        BindVitalityToCombatant();
        SubscribeRuntimeEvents();
        ConfigureOverlapFilter();
        EnsureAttackHitboxStartsDisabled();
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
        ClearHitReceiverCache();
    }

    private void OnValidate()
    {
        _configuration?.Clamp();
        ResolveRegisteredCombatant();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcessHitboxCollider(other);
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

    public void SetAttackHitboxEnabled(bool enabled)
    {
        if (_basicAttackHitbox == null)
        {
            return;
        }

        if (!enabled)
        {
            ClearHitReceiverCache();
        }

        _basicAttackHitbox.enabled = enabled;
    }

    public void ScanInitialHitboxOverlaps()
    {
        if (_basicAttackHitbox == null || !_basicAttackHitbox.enabled)
        {
            return;
        }

        _overlapBuffer.Clear();
        Physics2D.OverlapCollider(_basicAttackHitbox, _overlapFilter, _overlapBuffer);
        foreach (Collider2D other in _overlapBuffer)
        {
            ProcessHitboxCollider(other);
        }
    }

    public void ProcessHitboxCollider(Collider2D other)
    {
        if (other == null || _basicAttackHitbox == null || !_basicAttackHitbox.enabled)
        {
            return;
        }

        if (IsOwnedCollider(other))
        {
            return;
        }

        IDamageReceiver receiver = ResolveHitReceiver(other);
        if (receiver == null || ReferenceEquals(receiver, this))
        {
            return;
        }

        HitCandidateDetected?.Invoke(receiver, other.bounds.center);
    }

    public void ClearHitReceiverCache()
    {
        _hitReceiverCache.Clear();
    }

    private IDamageReceiver ResolveHitReceiver(Collider2D other)
    {
        if (_hitReceiverCache.TryGetValue(other, out IDamageReceiver cached))
        {
            return cached;
        }

        IDamageReceiver receiver = CombatHurtboxResolver.Resolve(other);
        if (receiver != null)
        {
            _hitReceiverCache[other] = receiver;
        }

        return receiver;
    }

    private bool IsOwnedCollider(Collider2D other)
    {
        Transform otherTransform = other.transform;
        return otherTransform == transform || otherTransform.IsChildOf(transform);
    }

    private void EnsureAttackHitboxStartsDisabled()
    {
        if (_basicAttackHitbox != null)
        {
            _basicAttackHitbox.enabled = false;
        }
    }

    private void ConfigureOverlapFilter()
    {
        _overlapFilter = ContactFilter2D.noFilter;
        _overlapFilter.useTriggers = true;

        int hurtboxLayer = LayerMask.NameToLayer("Hurtbox");
        if (hurtboxLayer >= 0)
        {
            _overlapFilter.SetLayerMask(1 << hurtboxLayer);
            _overlapFilter.useLayerMask = true;
        }
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
