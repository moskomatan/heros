using UnityEngine;

/// <summary>
/// Resolves an <see cref="IDamageReceiver"/> from a contacted hurtbox collider
/// via parent hierarchy lookup. Pure helper for EditMode tests and hitbox routing.
/// </summary>
public static class CombatHurtboxResolver
{
    public static IDamageReceiver Resolve(Collider2D collider)
    {
        if (collider == null)
        {
            return null;
        }

        return collider.GetComponentInParent<IDamageReceiver>();
    }
}
