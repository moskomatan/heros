using System;

public sealed class FixedRandomSource : IRandomSource
{
    public FixedRandomSource(float nextFloat)
    {
        _nextFloat = ClampToUnitInterval(nextFloat);
    }

    private readonly float _nextFloat;

    public float NextFloat()
    {
        return _nextFloat;
    }

    private static float ClampToUnitInterval(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value >= 1f)
        {
            return MathF.NextAfter(1f, 0f);
        }

        return value;
    }
}
