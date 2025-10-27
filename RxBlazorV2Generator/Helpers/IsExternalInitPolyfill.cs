// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for IsExternalInit to enable C# 9 init accessors and records in .NET Standard 2.0
/// See https://github.com/dotnet/roslyn/issues/45510
/// </summary>
internal static class IsExternalInit
{
}
