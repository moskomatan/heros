using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterCombat : MonoBehaviour, IDamageReceiver, ICombatVitality, IBasicAttackRequester
{
    [SerializeField] private CombatConfiguration _configuration = new CombatConfiguration();
    [SerializeField] private RegisteredCombatant _registeredCombatant;
    [SerializeField] private Collider2D _basicAttackHitbox;
    [SerializeField] private MonoBehaviour _attackInputSourceBehaviour;

    private HealthPool _healthPool;
    private DamageReceiver _damageReceiver;
    private ConfigurableMovementGate _movementGate;
    private IMovementState _movementState;
    private AttackExecutionTracker _executionTracker;
    private IRandomSource _randomSource;
    private ITeamRelationshipService _relationshipService;
    private BasicAttackRuntime _basicAttackRuntime;
    private IAttackInputSource _attackInputSource;
    private readonly Dictionary<Collider2D, IDamageReceiver> _hitReceiverCache = new();
    private readonly Dictionary<Collider2D, ICombatant> _hitCombatantCache = new();
    private readonly List<Collider2D> _overlapBuffer = new(8);
    private ContactFilter2D _overlapFilter;
    private float _basicAttackHitboxLocalAbsX;
    private bool _hasCachedHitboxLocalAbsX;

    public event Action<int, int> HealthChanged;

    public event Action Died;

    public event Action<DamageResult> DamageApplied;

    public event Action AttackStarted;

    public event Action AttackEnded;

    /// <summary>
    /// Raised when the basic attack hitbox contacts a resolvable damage receiver.
    /// </summary>
    public event Action<IDamageReceiver, ICombatant, Vector3> HitCandidateDetected;

    public RegisteredCombatant RegisteredCombatant => _registeredCombatant;

    public Collider2D BasicAttackHitbox => _basicAttackHitbox;

    public bool IsAlive => _healthPool != null && _healthPool.IsAlive;

    public int CurrentHealth => _healthPool != null ? _healthPool.CurrentHealth : 0;

    public int MaxHealth => _healthPool != null ? _healthPool.MaxHealth : 0;

    public BasicAttackPhase AttackPhase =>
        _basicAttackRuntime != null ? _basicAttackRuntime.Phase : BasicAttackPhase.Ready;

    private void Awake()
    {
        ResolveRegisteredCombatant();
        ResolveAttackInputSource();
        ConstructRuntimeObjects();
        BindVitalityToCombatant();
        SubscribeRuntimeEvents();
        ConfigureOverlapFilter();
        CacheAttackHitboxLocalAbsX();
        EnsureAttackHitboxStartsDisabled();
    }

    private void Start()
    {
        ResolveMovementDependencies();
    }

    private void Update()
    {
        if (_basicAttackRuntime != null)
        {
            _basicAttackRuntime.Tick(Time.deltaTime);
        }

        if (_attackInputSource != null && _attackInputSource.ConsumeBasicAttackPressed())
        {
            TryBasicAttack();
        }
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

        if (_attackInputSourceBehaviour != null && _attackInputSourceBehaviour is not IAttackInputSource)
        {
            Debug.LogWarning(
                $"{nameof(CharacterCombat)} on {name} expects {nameof(_attackInputSourceBehaviour)} to implement {nameof(IAttackInputSource)}.",
                this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcessHitboxCollider(other);
    }

    public void Initialize(ITeamRelationshipService relationshipService)
    {
        if (relationshipService == null)
        {
            Debug.LogError($"{nameof(CharacterCombat)} on {name} received a null relationship service.", this);
            return;
        }

        _relationshipService = relationshipService;
        ResolveRegisteredCombatant();
        ConstructAttackRuntime();
    }

    public DamageResult ReceiveDamage(in DamageRequest request)
    {
        return _damageReceiver.ReceiveDamage(in request);
    }

    public bool TryBasicAttack()
    {
        return _basicAttackRuntime != null && _basicAttackRuntime.TryBeginAttack();
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
            _basicAttackHitbox.enabled = false;
            return;
        }

        OrientAttackHitboxToFacing();
        _basicAttackHitbox.enabled = true;
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

        ICombatant combatant = ResolveHitCombatant(other);
        HitCandidateDetected?.Invoke(receiver, combatant, other.bounds.center);
    }

    public void ClearHitReceiverCache()
    {
        _hitReceiverCache.Clear();
        _hitCombatantCache.Clear();
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

    private ICombatant ResolveHitCombatant(Collider2D other)
    {
        if (_hitCombatantCache.TryGetValue(other, out ICombatant cached))
        {
            return cached;
        }

        ICombatant combatant = other.GetComponentInParent<ICombatant>();
        if (combatant != null)
        {
            _hitCombatantCache[other] = combatant;
        }

        return combatant;
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

    private void CacheAttackHitboxLocalAbsX()
    {
        if (_basicAttackHitbox == null || _hasCachedHitboxLocalAbsX)
        {
            return;
        }

        _basicAttackHitboxLocalAbsX = Mathf.Abs(_basicAttackHitbox.transform.localPosition.x);
        _hasCachedHitboxLocalAbsX = true;
    }

    private void OrientAttackHitboxToFacing()
    {
        if (_basicAttackHitbox == null)
        {
            return;
        }

        CacheAttackHitboxLocalAbsX();
        ResolveMovementDependencies();

        float facingSign = 1f;
        if (_movementState != null)
        {
            float facingX = _movementState.LastNonZeroDirection.x;
            if (Mathf.Abs(facingX) > 0.0001f)
            {
                facingSign = Mathf.Sign(facingX);
            }
        }

        Vector3 localPosition = _basicAttackHitbox.transform.localPosition;
        _basicAttackHitbox.transform.localPosition = new Vector3(
            _basicAttackHitboxLocalAbsX * facingSign,
            localPosition.y,
            localPosition.z);
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
        _executionTracker = new AttackExecutionTracker();
        _randomSource = new UnityRandomSource();
    }

    private void ConstructAttackRuntime()
    {
        if (_basicAttackRuntime != null || _relationshipService == null || _registeredCombatant == null)
        {
            return;
        }

        AttackHitValidator validator = new AttackHitValidator(_relationshipService, _executionTracker);
        IAttackHitbox hitbox = new CharacterAttackHitbox(this);
        _basicAttackRuntime = new BasicAttackRuntime(
            _configuration.BasicAttack,
            _registeredCombatant,
            () => IsAlive,
            hitbox,
            _randomSource,
            _executionTracker,
            validator,
            SetAttackMovementLock);

        HitCandidateDetected += _basicAttackRuntime.HandleHitCandidate;
        _basicAttackRuntime.AttackStarted += OnAttackStarted;
        _basicAttackRuntime.AttackEnded += OnAttackEnded;
    }

    private void SetAttackMovementLock(bool isLocked)
    {
        ResolveMovementDependencies();
        if (_movementGate == null)
        {
            return;
        }

        if (isLocked)
        {
            _movementGate.CanMove = false;
            return;
        }

        if (IsAlive)
        {
            _movementGate.CanMove = true;
        }
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

    private void ResolveAttackInputSource()
    {
        _attackInputSource = null;
        if (_attackInputSourceBehaviour == null)
        {
            return;
        }

        if (_attackInputSourceBehaviour is not IAttackInputSource attackInputSource)
        {
            Debug.LogError(
                $"{nameof(CharacterCombat)} on {name} requires {nameof(_attackInputSourceBehaviour)} to implement {nameof(IAttackInputSource)}.",
                this);
            return;
        }

        _attackInputSource = attackInputSource;
    }

    private void ResolveMovementDependencies()
    {
        if (_movementGate != null && _movementState != null)
        {
            return;
        }

        CombatCharacter combatCharacter = GetComponent<CombatCharacter>();
        if (combatCharacter == null)
        {
            return;
        }

        if (_movementGate == null)
        {
            _movementGate = combatCharacter.MovementGate;
        }

        if (_movementState == null)
        {
            _movementState = combatCharacter.MovementState;
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

        if (_basicAttackRuntime != null)
        {
            HitCandidateDetected -= _basicAttackRuntime.HandleHitCandidate;
            _basicAttackRuntime.AttackStarted -= OnAttackStarted;
            _basicAttackRuntime.AttackEnded -= OnAttackEnded;
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        HealthChanged?.Invoke(current, max);
    }

    private void OnDied()
    {
        _basicAttackRuntime?.Cancel();

        ResolveMovementDependencies();
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

    private void OnAttackStarted()
    {
        AttackStarted?.Invoke();
    }

    private void OnAttackEnded()
    {
        AttackEnded?.Invoke();
    }

    private sealed class CharacterAttackHitbox : IAttackHitbox
    {
        private readonly CharacterCombat _owner;

        public CharacterAttackHitbox(CharacterCombat owner)
        {
            _owner = owner;
        }

        public void SetEnabled(bool enabled)
        {
            _owner.SetAttackHitboxEnabled(enabled);
        }

        public void ScanInitialOverlaps()
        {
            _owner.ScanInitialHitboxOverlaps();
        }
    }
}
