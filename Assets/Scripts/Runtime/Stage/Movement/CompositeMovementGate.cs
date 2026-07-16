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
            for (int i = 0; i < _gates.Count; i++)
            {
                if (!_gates[i].CanMove)
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

            for (int i = 0; i < _gates.Count; i++)
            {
                multiplier *= _gates[i].SpeedMultiplier;
            }

            return multiplier;
        }
    }
}
