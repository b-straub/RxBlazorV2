using System.Collections;
using System.Collections.Immutable;

namespace RxBlazorV2Generator.Helpers;

/// <summary>
/// Wrapper around ImmutableArray that provides structural equality for use in IncrementalValueProviders.
/// Based on guidance from https://andrewlock.net/creating-a-source-generator-part-9-avoiding-performance-pitfalls-in-incremental-generators/
/// </summary>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _array;

    public EquatableArray(ImmutableArray<T> array)
    {
        _array = array;
    }

    public EquatableArray(IEnumerable<T> items)
    {
        _array = items.ToImmutableArray();
    }

    public EquatableArray(params T[] items)
    {
        _array = ImmutableArray.Create(items);
    }

    public T this[int index] => _array[index];

    public int Length => _array.IsDefault ? 0 : _array.Length;

    public bool IsEmpty => _array.IsDefaultOrEmpty;

    public bool Equals(EquatableArray<T> other)
    {
        // If both are default, they're equal
        if (_array.IsDefault && other._array.IsDefault)
        {
            return true;
        }

        // If only one is default, they're not equal
        if (_array.IsDefault || other._array.IsDefault)
        {
            return false;
        }

        // Different lengths means not equal
        if (_array.Length != other._array.Length)
        {
            return false;
        }

        // Compare elements
        for (int i = 0; i < _array.Length; i++)
        {
            if (!_array[i].Equals(other._array[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        if (_array.IsDefault)
        {
            return 0;
        }

        var hashCode = new HashCode();
        foreach (var item in _array)
        {
            hashCode.Add(item);
        }
        return hashCode.ToHashCode();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return (_array.IsDefault ? ImmutableArray<T>.Empty : _array).AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }

    public static implicit operator EquatableArray<T>(ImmutableArray<T> array)
    {
        return new EquatableArray<T>(array);
    }

    public ImmutableArray<T> ToImmutableArray()
    {
        return _array.IsDefault ? ImmutableArray<T>.Empty : _array;
    }
}
