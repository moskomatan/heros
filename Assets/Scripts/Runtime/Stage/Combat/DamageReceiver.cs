using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public sealed class DamageReceiver : IDamageReceiver
{
    // ReceiveDamage queues eligible hits for ResolvePendingBatch. Accepted calls return a
    // placeholder result (FinalDamage 0, current RemainingHealth). Real values come from
    // ResolvePendingBatch and the DamageApplied event after the batch commits.
    public DamageReceiver(HealthPool healthPool, IDamageMitigation mitigation)
    {
        _healthPool = healthPool ?? throw new ArgumentNullException(nameof(healthPool));
        _mitigation = mitigation ?? throw new ArgumentNullException(nameof(mitigation));
    }

    private readonly HealthPool _healthPool;
    private readonly IDamageMitigation _mitigation;
    private readonly List<PendingDamage> _currentBatch = new();
    private readonly List<PendingDamage> _nextBatch = new();
    private bool _isResolving;

    public bool IsAlive => _healthPool.IsAlive;

    public event Action<DamageResult> DamageApplied;

    public DamageResult ReceiveDamage(in DamageRequest request)
    {
        if (!CanAcceptAtQueueTime(in request))
        {
            return CreateRejectedResult(in request);
        }

        PendingDamage pending = new PendingDamage(in request);

        if (_isResolving)
        {
            _nextBatch.Add(pending);
        }
        else
        {
            _currentBatch.Add(pending);
        }

        return CreateQueuedAcknowledgement(in request);
    }

    public IReadOnlyList<DamageResult> ResolvePendingBatch()
    {
        if (_currentBatch.Count == 0)
        {
            return Array.Empty<DamageResult>();
        }

        _isResolving = true;

        _currentBatch.Sort(ComparePendingDamage);

        int healthBefore = _healthPool.CurrentHealth;
        var mitigatedHits = new List<MitigatedHit>(_currentBatch.Count);
        int totalFinalDamage = 0;

        foreach (PendingDamage pending in _currentBatch)
        {
            MitigationResult mitigation = _mitigation.Mitigate(in pending.Request);
            mitigatedHits.Add(new MitigatedHit(pending.Request, mitigation));
            totalFinalDamage += mitigation.FinalDamage;
        }

        _healthPool.ApplyDamage(totalFinalDamage);

        int runningDamage = 0;
        var results = new List<DamageResult>(mitigatedHits.Count);

        foreach (MitigatedHit hit in mitigatedHits)
        {
            runningDamage += hit.Mitigation.FinalDamage;
            int remainingHealth = Math.Max(0, healthBefore - runningDamage);
            bool wasLethal = healthBefore > 0 && remainingHealth == 0;

            results.Add(new DamageResult(
                wasApplied: true,
                rawDamage: hit.Request.RawDamage,
                normalDefenseApplied: hit.Mitigation.NormalDefenseApplied,
                criticalDefenseApplied: hit.Mitigation.CriticalDefenseApplied,
                finalDamage: hit.Mitigation.FinalDamage,
                remainingHealth: remainingHealth,
                wasCritical: hit.Request.IsCritical,
                wasLethal: wasLethal));
        }

        _currentBatch.Clear();

        foreach (DamageResult result in results)
        {
            DamageApplied?.Invoke(result);
        }

        foreach (PendingDamage deferred in _nextBatch)
        {
            _currentBatch.Add(deferred);
        }

        _nextBatch.Clear();
        _isResolving = false;

        return results;
    }

    private bool CanAcceptAtQueueTime(in DamageRequest request)
    {
        if (!_healthPool.IsAlive)
        {
            return false;
        }

        if (request.Attacker == null)
        {
            return false;
        }

        return true;
    }

    private DamageResult CreateRejectedResult(in DamageRequest request)
    {
        return new DamageResult(
            wasApplied: false,
            rawDamage: request.RawDamage,
            normalDefenseApplied: 0,
            criticalDefenseApplied: 0,
            finalDamage: 0,
            remainingHealth: _healthPool.CurrentHealth,
            wasCritical: request.IsCritical,
            wasLethal: false);
    }

    private DamageResult CreateQueuedAcknowledgement(in DamageRequest request)
    {
        return new DamageResult(
            wasApplied: true,
            rawDamage: request.RawDamage,
            normalDefenseApplied: 0,
            criticalDefenseApplied: 0,
            finalDamage: 0,
            remainingHealth: _healthPool.CurrentHealth,
            wasCritical: request.IsCritical,
            wasLethal: false);
    }

    // Stable order: AttackExecutionId ascending, then attacker identity hash as tie breaker.
    // RuntimeHelpers.GetHashCode uses reference identity for objects, which is stable for
    // the lifetime of each attacker instance and sufficient for deterministic EditMode tests.
    private static int ComparePendingDamage(PendingDamage left, PendingDamage right)
    {
        int executionIdComparison = left.Request.AttackExecutionId.CompareTo(right.Request.AttackExecutionId);
        if (executionIdComparison != 0)
        {
            return executionIdComparison;
        }

        int leftAttackerHash = RuntimeHelpers.GetHashCode(left.Request.Attacker);
        int rightAttackerHash = RuntimeHelpers.GetHashCode(right.Request.Attacker);
        return leftAttackerHash.CompareTo(rightAttackerHash);
    }

    private readonly struct PendingDamage
    {
        public PendingDamage(in DamageRequest request)
        {
            Request = request;
        }

        public DamageRequest Request { get; }
    }

    private readonly struct MitigatedHit
    {
        public MitigatedHit(in DamageRequest request, MitigationResult mitigation)
        {
            Request = request;
            Mitigation = mitigation;
        }

        public DamageRequest Request { get; }

        public MitigationResult Mitigation { get; }
    }
}
