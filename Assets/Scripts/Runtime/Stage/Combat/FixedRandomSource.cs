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
            return LargestFloatBelowOne();
        }

        return value;
    }

    // MathF.NextAfter is not available on Unity's .NET Standard 2.1 profile.
    // Decrement the IEEE-754 bit pattern of 1f to get the largest representable float below 1.
    private static float LargestFloatBelowOne()
    {
        int bits = BitConverter.ToInt32(BitConverter.GetBytes(1f), 0);
        return BitConverter.ToSingle(BitConverter.GetBytes(bits - 1), 0);
    }
}
