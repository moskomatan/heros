using UnityEngine;

public sealed class PlayerMovementInputSource : MonoBehaviour, IMovementInputSource
{
    [SerializeField] private Vector2 _direction;

    public Vector2 Direction
    {
        get => _direction;
        set => _direction = value;
    }

    public Vector2 GetDirection()
    {
        return _direction;
    }
}
