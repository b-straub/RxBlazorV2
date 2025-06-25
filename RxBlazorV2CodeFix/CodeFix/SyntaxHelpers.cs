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
using System.Linq;

namespace RxBlazorV2CodeFix.CodeFix;

    internal static class SyntaxHelpers
    {
        private static bool True(this bool? value)
        {
            return value.GetValueOrDefault(false);
        }
        
        public static SyntaxList<TNode> RemoveKeepTrivia<TNode>(this SyntaxList<TNode> list, TNode node) where TNode : SyntaxNode
        {
            if (node is null)
            {
                return list;
            }

            var firstNode = (node?.Equals(list.First())).True();
            var lastNode = (node?.Equals(list.Last())).True();

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

        public static SeparatedSyntaxList<TNode> RemoveKeepTrivia<TNode>(this SeparatedSyntaxList<TNode> list, TNode node) where TNode : SyntaxNode
        {
            if (node is null)
            {
                return list;
            }

            var firstNode = (node?.Equals(list.First())).True();
            var lastNode = (node?.Equals(list.Last())).True();

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
}
