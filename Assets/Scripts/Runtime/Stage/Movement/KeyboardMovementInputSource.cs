using UnityEngine;
using UnityEngine.InputSystem;

public sealed class KeyboardMovementInputSource : MonoBehaviour, IMovementInputSource
{
    public Vector2 GetDirection()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        return new Vector2(horizontal, vertical);
    }
}
