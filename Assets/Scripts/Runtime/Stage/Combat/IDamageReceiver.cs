public interface IDamageReceiver
{
    bool IsAlive { get; }

    DamageResult ReceiveDamage(in DamageRequest request);
}
