public sealed class AlwaysAllowMovementGate : IMovementGate
{
    public bool CanMove => true;
    public float SpeedMultiplier => 1f;
}
