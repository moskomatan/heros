using System;
using UnityEngine;

public enum BasicAttackPhase
{
    Ready,
    Active,
    Cooldown
}

public sealed class BasicAttackRuntime
{
    public BasicAttackRuntime(
        CombatConfiguration.BasicAttackSettings settings,
        ICombatant attacker,
        Func<bool> isAlive,
        IAttackHitbox hitbox,
        IRandomSource random,
        AttackExecutionTracker executionTracker,
        AttackHitValidator hitValidator,
        Action<bool> setAttackMovementLock)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        _damage = settings.Damage;
        _criticalChance = settings.CriticalChance;
        _criticalDamageMultiplier = settings.CriticalDamageMultiplier;
        _activeTime = settings.ActiveTime;
        _cooldown = settings.Cooldown;
        _attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        _isAlive = isAlive ?? throw new ArgumentNullException(nameof(isAlive));
        _hitbox = hitbox ?? throw new ArgumentNullException(nameof(hitbox));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _executionTracker = executionTracker
            ?? throw new ArgumentNullException(nameof(executionTracker));
        _hitValidator = hitValidator ?? throw new ArgumentNullException(nameof(hitValidator));
        _setAttackMovementLock = setAttackMovementLock
            ?? throw new ArgumentNullException(nameof(setAttackMovementLock));
    }

    private readonly int _damage;
    private readonly float _criticalChance;
    private readonly float _criticalDamageMultiplier;
    private readonly float _activeTime;
    private readonly float _cooldown;
    private readonly ICombatant _attacker;
    private readonly Func<bool> _isAlive;
    private readonly IAttackHitbox _hitbox;
    private readonly IRandomSource _random;
    private readonly AttackExecutionTracker _executionTracker;
    private readonly AttackHitValidator _hitValidator;
    private readonly Action<bool> _setAttackMovementLock;

    private BasicAttackPhase _phase = BasicAttackPhase.Ready;
    private float _phaseElapsed;
    private uint _nextExecutionId = 1u;
    private uint _currentExecutionId;

    public BasicAttackPhase Phase => _phase;

    public uint CurrentExecutionId => _currentExecutionId;

    public bool IsActive => _phase == BasicAttackPhase.Active;

    public bool TryBeginAttack()
    {
        if (!_isAlive())
        {
            return false;
        }

        if (_phase != BasicAttackPhase.Ready)
        {
            return false;
        }

        _currentExecutionId = _nextExecutionId;
        _nextExecutionId++;
        _executionTracker.BeginExecution(_currentExecutionId);
        _phase = BasicAttackPhase.Active;
        _phaseElapsed = 0f;
        _hitbox.SetEnabled(true);
        _hitbox.ScanInitialOverlaps();
        _setAttackMovementLock(true);

        if (_activeTime <= 0f)
        {
            EnterCooldown();
        }

        return true;
    }

    public void Tick(float deltaTime)
    {
        if (_phase == BasicAttackPhase.Ready)
        {
            return;
        }

        if (deltaTime < 0f)
        {
            deltaTime = 0f;
        }

        _phaseElapsed += deltaTime;

        if (_phase == BasicAttackPhase.Active)
        {
            if (_phaseElapsed >= _activeTime)
            {
                EnterCooldown();
            }

            return;
        }

        if (_phase == BasicAttackPhase.Cooldown && _phaseElapsed >= _cooldown)
        {
            _phase = BasicAttackPhase.Ready;
            _phaseElapsed = 0f;
        }
    }

    public void HandleHitCandidate(IDamageReceiver receiver, ICombatant defender, Vector3 hitPoint)
    {
        if (_phase != BasicAttackPhase.Active)
        {
            return;
        }

        if (!_hitValidator.IsValidHit(_attacker, defender, receiver))
        {
            return;
        }

        _executionTracker.RecordHit(defender);

        bool isCritical = _random.NextFloat() < _criticalChance;
        int rawDamage = _damage;
        if (isCritical)
        {
            rawDamage = Mathf.RoundToInt(_damage * _criticalDamageMultiplier);
        }

        DamageRequest request = new DamageRequest(
            _attacker,
            _currentExecutionId,
            rawDamage,
            isCritical,
            hitPoint);
        receiver.ReceiveDamage(in request);
    }

    public void Cancel()
    {
        if (_phase == BasicAttackPhase.Active)
        {
            _hitbox.SetEnabled(false);
            _setAttackMovementLock(false);
        }

        _phase = BasicAttackPhase.Ready;
        _phaseElapsed = 0f;
    }

    private void EnterCooldown()
    {
        _hitbox.SetEnabled(false);
        _setAttackMovementLock(false);
        _phase = BasicAttackPhase.Cooldown;
        _phaseElapsed = 0f;

        if (_cooldown <= 0f)
        {
            _phase = BasicAttackPhase.Ready;
        }
    }
}
