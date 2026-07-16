using UnityEngine;

public sealed class TransformMovementMotor : ICharacterMovementMotor
{
    private readonly Transform _transform;

    public TransformMovementMotor(Transform transform)
    {
        _transform = transform;
    }

    public void Move(Vector2 velocity, float deltaTime)
    {
        Vector3 displacement = new Vector3(velocity.x, velocity.y, 0f) * deltaTime;
        _transform.position += displacement;
    }
}
