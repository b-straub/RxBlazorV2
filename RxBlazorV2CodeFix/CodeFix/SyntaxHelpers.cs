/*
SyntaxHelpers.cs

Copyright(c) 2024 Bernhard Straub

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace RxBlazorV2CodeFix.CodeFix;

internal static class SyntaxHelpers
{
    private static bool True(this bool? value)
    {
        return value.GetValueOrDefault(false);
    }

    public static SyntaxList<TNode> RemoveKeepTrivia<TNode>(this SyntaxList<TNode> list, TNode node)
        where TNode : SyntaxNode
    {
        var firstNode = node.Equals(list.First());
        var lastNode = node.Equals(list.Last());

        var newList = list.Remove(node!);

        if (firstNode)
        {
            var newFirst = newList.FirstOrDefault()?.WithLeadingTrivia(node!.GetLeadingTrivia());
            if (newFirst is not null)
            {
                newList = newList.Replace(newList.First(), newFirst);
            }
        }

        if (lastNode)
        {
            var newLast = newList.LastOrDefault()?.WithTrailingTrivia(node!.GetTrailingTrivia());
            if (newLast is not null)
            {
                newList = newList.Replace(newList.Last(), newLast);
            }
        }

        return newList;
    }

    public static SeparatedSyntaxList<TNode> RemoveKeepTrivia<TNode>(this SeparatedSyntaxList<TNode> list, TNode node)
        where TNode : SyntaxNode
    {
        var firstNode = node.Equals(list.First());
        var lastNode = node.Equals(list.Last());

        var newList = list.Remove(node!);

        if (firstNode)
        {
            var newFirst = newList.FirstOrDefault()?.WithLeadingTrivia(node!.GetLeadingTrivia());
            if (newFirst is not null)
            {
                newList = newList.Replace(newList.First(), newFirst);
            }
        }

        if (lastNode)
        {
            var newLast = newList.LastOrDefault()?.WithTrailingTrivia(node!.GetTrailingTrivia());
            if (newLast is not null)
            {
                newList = newList.Replace(newList.Last(), newLast);
            }
        }

        return newList;
    }

    /// <summary>
    /// Removes an attribute from a class or property declaration, preserving trivia.
    /// If the attribute is the only one in its attribute list, removes the entire list.
    /// Otherwise, removes just the attribute.
    /// </summary>
    public static SyntaxNode RemoveAttributeFromClass(
        SyntaxNode root,
        AttributeSyntax attribute)
    {
        var attributeList = attribute.Parent as AttributeListSyntax;
        if (attributeList is null)
        {
            return root;
        }

        if (attributeList.Attributes.Count == 1)
        {
            if (attributeList.Parent is ClassDeclarationSyntax classDecl)
            {
                var newAttributeLists = classDecl.AttributeLists.RemoveKeepTrivia(attributeList);
                var newClassDecl = classDecl.WithAttributeLists(newAttributeLists);
                return root.ReplaceNode(classDecl, newClassDecl);
            }
            else if (attributeList.Parent is PropertyDeclarationSyntax propertyDecl)
            {
                var newAttributeLists = propertyDecl.AttributeLists.RemoveKeepTrivia(attributeList);
                var newPropertyDecl = propertyDecl.WithAttributeLists(newAttributeLists);
                return root.ReplaceNode(propertyDecl, newPropertyDecl);
            }
            else
            {
                return root;
            }
        }
        else
        {
            var newAttributes = attributeList.Attributes.Remove(attribute);
            var newAttributeList = attributeList.WithAttributes(newAttributes);
            return root.ReplaceNode(attributeList, newAttributeList);
        }
    }

    /// <summary>
    /// Adds using directives to a compilation unit if they don't already exist.
    /// Supports both single and multiple namespaces. Preserves line endings from existing usings.
    /// </summary>
    public static SyntaxNode AddUsingDirectives(
        SyntaxNode root,
        params string[] namespaces)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return root;
        }

        var existingUsings = new HashSet<string>(
            compilationUnit.Usings
                .Select(u => u.Name?.ToString())
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>());

        var newUsings = new List<UsingDirectiveSyntax>();

        foreach (var usingNamespace in namespaces)
        {
            if (!existingUsings.Contains(usingNamespace))
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName(usingNamespace));

                if (compilationUnit.Usings.Count > 0)
                {
                    var lastUsing = compilationUnit.Usings.Last();
                    usingDirective = usingDirective.WithTrailingTrivia(lastUsing.GetTrailingTrivia());
                }

                newUsings.Add(usingDirective);
            }
        }

        if (newUsings.Count > 0)
        {
            var allUsings = compilationUnit.Usings.AddRange(newUsings);
            return compilationUnit.WithUsings(allUsings);
        }

        return root;
    }

    /// <summary>
    /// Extracts the type symbol from an attribute that references a type.
    /// Supports both generic syntax (Attribute<T>) and typeof syntax (Attribute(typeof(T))).
    /// </summary>
    public static INamedTypeSymbol? ExtractTypeFromAttribute(
        AttributeSyntax attribute,
        SemanticModel semanticModel)
    {
        if (attribute.Name is GenericNameSyntax genericName &&
            genericName.TypeArgumentList?.Arguments.Count > 0)
        {
            var typeArgument = genericName.TypeArgumentList.Arguments.First();
            var typeInfo = semanticModel.GetTypeInfo(typeArgument);
            return typeInfo.Type as INamedTypeSymbol;
        }

        if (attribute.ArgumentList?.Arguments.Count > 0)
        {
            var firstArgument = attribute.ArgumentList.Arguments.First();
            if (firstArgument.Expression is TypeOfExpressionSyntax typeOfExpression)
            {
                var typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type);
                if (typeInfo.Type is INamedTypeSymbol namedType)
                {
                    return namedType.IsGenericType && namedType.TypeArguments.Length > 0
                        ? namedType.ConstructedFrom
                        : namedType;
                }
            }
        }

        return null;
    }
}