using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

/// <summary>
/// Provides a code fix for RXBG091 (Error formatter method not found): scaffolds a stub
/// <c>private string {Name}(Exception ex) =&gt; $"Failed to {humanCommand}: {{ex.Message}}";</c>
/// inside the same partial class, immediately after the command property.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingErrorFormatterCodeFixProvider)), Shared]
public class MissingErrorFormatterCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.ErrorFormatterMethodNotFoundError.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue("CommandProperty", out var commandPropertyName) ||
                !diagnostic.Properties.TryGetValue("FormatterName", out var formatterName) ||
                string.IsNullOrEmpty(commandPropertyName) ||
                string.IsNullOrEmpty(formatterName))
            {
                continue;
            }

            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var commandProperty = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (commandProperty is null)
            {
                continue;
            }

            var action = CodeAction.Create(
                title: $"Generate stub formatter method '{formatterName}'",
                createChangedDocument: ct => InsertStubAsync(
                    context.Document, root, commandProperty, commandPropertyName!, formatterName!, ct),
                equivalenceKey: $"GenerateErrorFormatter:{formatterName}");

            context.RegisterCodeFix(action, diagnostic);
        }
    }

    private static Task<Document> InsertStubAsync(
        Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax commandProperty,
        string commandPropertyName,
        string formatterName,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var humanLabel = HumanizeCommandName(commandPropertyName);
        var stubText = $"    private string {formatterName}(System.Exception ex) =>\n" +
                       $"        $\"Failed to {humanLabel}: {{ex.Message}}\";\n";

        // Use LF-only end-of-line trivia so the inserted member matches the rest of the file's
        // line endings (avoids mixed CRLF/LF in the output of code-fix tests on Unix CI).
        var newline = SyntaxFactory.ElasticEndOfLine("\n");
        var stubMember = SyntaxFactory.ParseMemberDeclaration(stubText)!
            .WithLeadingTrivia(newline)
            .WithTrailingTrivia(newline);

        var classDecl = commandProperty.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDecl is null)
        {
            return Task.FromResult(document);
        }

        var insertIndex = classDecl.Members.IndexOf(commandProperty) + 1;
        var newMembers = classDecl.Members.Insert(insertIndex, stubMember);
        var newClass = classDecl.WithMembers(newMembers);
        var newRoot = root.ReplaceNode(classDecl, newClass);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    /// <summary>
    /// Converts a command property name (e.g. "LoadContactsCommand") into a lowercased,
    /// space-separated label ("load contacts") suitable for embedding in a "Failed to X" sentence.
    /// </summary>
    private static string HumanizeCommandName(string commandPropertyName)
    {
        var trimmed = commandPropertyName.EndsWith("Command")
            ? commandPropertyName.Substring(0, commandPropertyName.Length - "Command".Length)
            : commandPropertyName;

        if (trimmed.Length == 0)
        {
            return "execute command";
        }

        var sb = new StringBuilder(trimmed.Length + 4);
        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (i > 0 && char.IsUpper(c))
            {
                sb.Append(' ');
            }
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
