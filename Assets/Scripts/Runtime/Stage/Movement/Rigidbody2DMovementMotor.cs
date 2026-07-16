using UnityEngine;

public sealed class Rigidbody2DMovementMotor : ICharacterMovementMotor
{
    private readonly Rigidbody2D _rigidbody;

    public Rigidbody2DMovementMotor(Rigidbody2D rigidbody)
    {
        _rigidbody = rigidbody;
    }

    public void Move(Vector2 velocity, float deltaTime)
    {
        Vector2 nextPosition = _rigidbody.position + velocity * deltaTime;
        _rigidbody.MovePosition(nextPosition);
    }
}
