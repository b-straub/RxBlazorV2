using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RxBlazorV2Generator.Helpers;

/// <summary>
/// Polyfill for ArgumentNullException.ThrowIfNull (netstandard2.0 compatibility)
/// </summary>
public static class NullableHelpers
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.
    /// </summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter.</param>
    public static void ThrowIfNull([NotNull] this object? argument, 
        [CallerMemberName] string paramName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (argument is null)
        {
            throw new ArgumentNullException($"{paramName} at {sourceFilePath}:{sourceLineNumber}");
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null,
    /// returning the non-null value if valid.
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <returns>The non-null argument.</returns>
    public static T ThrowIfNullReturn<T>([NotNull] this T? argument,
        [CallerMemberName] string paramName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        where T : class
    {
        return argument ?? throw new ArgumentNullException($"{paramName} at {sourceFilePath}:{sourceLineNumber}");
    }
    
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? data) {
        return string.IsNullOrEmpty(data);
    }
}
