public sealed class ConfigurableMovementGate : IMovementGate
{
    public bool CanMove { get; set; } = true;
    public float SpeedMultiplier { get; set; } = 1f;
}
