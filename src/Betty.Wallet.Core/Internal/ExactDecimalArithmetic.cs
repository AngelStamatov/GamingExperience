using System.Numerics;

namespace Betty.Wallet.Core.Internal;

internal static class ExactDecimalArithmetic
{
    private static readonly BigInteger MaximumCoefficient = (BigInteger.One << 96) - 1;

    internal static decimal Sum(decimal first, decimal second, decimal third = 0m)
    {
        var (firstCoefficient, firstScale) = Decompose(first);
        var (secondCoefficient, secondScale) = Decompose(second);
        var (thirdCoefficient, thirdScale) = Decompose(third);
        var scale = Math.Max(firstScale, Math.Max(secondScale, thirdScale));

        var coefficient = firstCoefficient * BigInteger.Pow(10, scale - firstScale)
            + secondCoefficient * BigInteger.Pow(10, scale - secondScale)
            + thirdCoefficient * BigInteger.Pow(10, scale - thirdScale);

        return ToDecimal(coefficient, scale);
    }

    private static (BigInteger Coefficient, int Scale) Decompose(decimal value)
    {
        var bits = decimal.GetBits(value);
        var coefficient = (BigInteger)(uint)bits[0]
            | ((BigInteger)(uint)bits[1] << 32)
            | ((BigInteger)(uint)bits[2] << 64);
        if ((bits[3] & int.MinValue) != 0)
        {
            coefficient = -coefficient;
        }

        return (coefficient, (bits[3] >> 16) & 0xff);
    }

    private static decimal ToDecimal(BigInteger coefficient, int scale)
    {
        // Only trailing zeros can be removed without changing the exact result.
        while (scale > 0 && coefficient % 10 == 0)
        {
            coefficient /= 10;
            scale--;
        }

        var magnitude = BigInteger.Abs(coefficient);
        if (magnitude > MaximumCoefficient)
        {
            throw new OverflowException("The exact result cannot be represented as decimal.");
        }

        var low = unchecked((int)(uint)(magnitude & uint.MaxValue));
        var middle = unchecked((int)(uint)((magnitude >> 32) & uint.MaxValue));
        var high = unchecked((int)(uint)((magnitude >> 64) & uint.MaxValue));

        return new decimal(low, middle, high, coefficient.Sign < 0, (byte)scale);
    }
}
