using UnityEngine;

public interface IMovementState
{
    Vector2 Direction { get; }
    Vector2 LastNonZeroDirection { get; }
    Vector2 Velocity { get; }
    bool IsMoving { get; }
    float NormalizedSpeed { get; }
}
