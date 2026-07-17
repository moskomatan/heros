using UnityEngine;

public sealed class UnityRandomSource : IRandomSource
{
    public float NextFloat()
    {
        return Random.value;
    }
}
