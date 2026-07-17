using System.Collections.Generic;

public sealed class CompositeMovementGate : IMovementGate
{
    private readonly IReadOnlyList<IMovementGate> _gates;

    public CompositeMovementGate(params IMovementGate[] gates)
    {
        _gates = gates;
    }

    public bool CanMove
    {
        get
        {
            foreach (IMovementGate gate in _gates)
            {
                if (!gate.CanMove)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public float SpeedMultiplier
    {
        get
        {
            float multiplier = 1f;

            foreach (IMovementGate gate in _gates)
            {
                multiplier *= gate.SpeedMultiplier;
            }

            return multiplier;
        }
    }
}
