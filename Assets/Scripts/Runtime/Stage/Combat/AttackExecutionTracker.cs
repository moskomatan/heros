using System.Collections.Generic;

public sealed class AttackExecutionTracker
{
    private readonly HashSet<ICombatant> _hitTargets = new();
    private uint _currentExecutionId;

    public uint CurrentExecutionId => _currentExecutionId;

    public void BeginExecution(uint executionId)
    {
        _currentExecutionId = executionId;
        _hitTargets.Clear();
    }

    public bool HasHit(ICombatant target)
    {
        return target != null && _hitTargets.Contains(target);
    }

    public void RecordHit(ICombatant target)
    {
        if (target == null)
        {
            return;
        }

        _hitTargets.Add(target);
    }
}
