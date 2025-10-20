using System.Text.RegularExpressions;

namespace RxBlazorV2Generator.Extensions;

public static class StringExtensions
{
    private static readonly Regex GenericTypeRegex = new(@"<([^<>]+)>", RegexOptions.Compiled);

    /// <summary>
    /// Converts a generic type name from display format to metadata format.
    /// Examples:
    /// - "MyClass&lt;T&gt;" → "MyClass`1"
    /// - "MyClass&lt;T1, T2&gt;" → "MyClass`2"
    /// - "Namespace.MyClass&lt;T1, T2, T3&gt;" → "Namespace.MyClass`3"
    /// </summary>
    public static string ToMetadataTypeName(this string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName) || !typeName.Contains('<'))
        {
            return typeName;
        }

        return GenericTypeRegex.Replace(typeName, match =>
        {
            var genericPart = match.Groups[1].Value;
            var count = genericPart.Count(c => c == ',') + 1;
            return $"`{count}";
        });
    }
}