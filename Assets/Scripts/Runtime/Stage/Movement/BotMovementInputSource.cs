using UnityEngine;

public sealed class BotMovementInputSource : MonoBehaviour, IMovementInputSource, IChaseTarget
{
    [SerializeField] private Transform _target;

    public Transform Target
    {
        get => _target;
        set => _target = value;
    }

    public Vector2 GetDirection()
    {
        if (_target == null)
        {
            return Vector2.zero;
        }

        Vector2 offset = _target.position - transform.position;

        if (offset.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        return offset.normalized;
    }
}
