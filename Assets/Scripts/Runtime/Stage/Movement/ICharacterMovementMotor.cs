using UnityEngine;

public interface ICharacterMovementMotor
{
    void Move(Vector2 velocity, float deltaTime);
}
