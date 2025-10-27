// ReSharper disable once CheckNamespace
#if NETSTANDARD2_0
namespace System;

/// <summary>
/// Polyfill for System.HashCode to support .NET Standard 2.0
/// Simplified version for combining hash codes
/// </summary>
internal struct HashCode
{
    private int _hashCode;

    public void Add<T>(T value)
    {
        _hashCode = unchecked((_hashCode * 31) + (value?.GetHashCode() ?? 0));
    }

    public int ToHashCode()
    {
        return _hashCode;
    }

    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        var hash = new HashCode();
        hash.Add(value1);
        hash.Add(value2);
        return hash.ToHashCode();
    }

    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        var hash = new HashCode();
        hash.Add(value1);
        hash.Add(value2);
        hash.Add(value3);
        return hash.ToHashCode();
    }

    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        var hash = new HashCode();
        hash.Add(value1);
        hash.Add(value2);
        hash.Add(value3);
        hash.Add(value4);
        return hash.ToHashCode();
    }

    public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
    {
        var hash = new HashCode();
        hash.Add(value1);
        hash.Add(value2);
        hash.Add(value3);
        hash.Add(value4);
        hash.Add(value5);
        return hash.ToHashCode();
    }

    public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
    {
        var hash = new HashCode();
        hash.Add(value1);
        hash.Add(value2);
        hash.Add(value3);
        hash.Add(value4);
        hash.Add(value5);
        hash.Add(value6);
        return hash.ToHashCode();
    }
}
#endif
