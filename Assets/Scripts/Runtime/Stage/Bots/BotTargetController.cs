using UnityEngine;

public sealed class BotTargetController
{
    private readonly ICombatant _observer;
    private readonly IChaseTarget _chaseTarget;
    private readonly ICombatantRegistry _registry;
    private readonly ITargetSelector _targetSelector;
    private readonly ITeamRelationshipService _relationshipService;
    private readonly float _reevaluateInterval;

    private ICombatant _currentTarget;
    private float _reevaluateTimer;

    public BotTargetController(
        ICombatant observer,
        IChaseTarget chaseTarget,
        ICombatantRegistry registry,
        ITargetSelector targetSelector,
        ITeamRelationshipService relationshipService,
        float reevaluateInterval = 0.25f)
    {
        _observer = observer;
        _chaseTarget = chaseTarget;
        _registry = registry;
        _targetSelector = targetSelector;
        _relationshipService = relationshipService;
        _reevaluateInterval = reevaluateInterval > 0f ? reevaluateInterval : 0.25f;
    }

    public void Tick(float deltaTime)
    {
        if (!IsAlive(_observer) ||
            !IsAlive(_chaseTarget) ||
            _registry == null ||
            _targetSelector == null ||
            _relationshipService == null)
        {
            return;
        }

        if (IsAlive(_currentTarget) && !IsValidTarget(_currentTarget))
        {
            ClearTarget();
        }

        _reevaluateTimer -= deltaTime;
        if (_reevaluateTimer > 0f)
        {
            return;
        }

        _reevaluateTimer = _reevaluateInterval;
        EvaluateTarget();
    }

    public void Stop()
    {
        ClearTarget();
        _reevaluateTimer = 0f;
    }

    private void EvaluateTarget()
    {
        ICombatant selected = _targetSelector.SelectTarget(
            _observer,
            _registry.Combatants,
            _relationshipService);

        _currentTarget = selected;
        _chaseTarget.Target = IsAlive(selected) ? selected.TargetTransform : null;
    }

    private bool IsValidTarget(ICombatant target)
    {
        if (!IsAlive(target) || !target.IsTargetable || !IsAlive(_observer))
        {
            return false;
        }

        return _relationshipService.AreEnemies(_observer.TeamMember, target.TeamMember);
    }

    private void ClearTarget()
    {
        _currentTarget = null;

        if (IsAlive(_chaseTarget))
        {
            _chaseTarget.Target = null;
        }
    }

    private static bool IsAlive(object value)
    {
        if (value == null)
        {
            return false;
        }

        if (value is Object unityObject)
        {
            return unityObject != null;
        }

        return true;
    }
}
