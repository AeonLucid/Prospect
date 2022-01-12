using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Prospect.Unreal.Generator
{
    internal class NetControlMessageParser
    {
        internal IReadOnlyList<NetControlMessageEntry> Parse(ImmutableArray<AttributeSyntax> attributes)
        {
            var entries = new List<NetControlMessageEntry>();

            foreach (var attribute in attributes)
            {
                var attributeParams = attribute.ArgumentList.Arguments;
                var attributeEntry = new NetControlMessageEntry();

                attributeEntry.Name = ((LiteralExpressionSyntax)attributeParams[0].Expression).Token.ValueText; // StringLiteralExpression
                attributeEntry.Index = (int)((LiteralExpressionSyntax)attributeParams[1].Expression).Token.Value; // NumericLiteralExpression

                for (int i = 2; i < attributeParams.Count; i++)
                {
                    var paramType = ((TypeOfExpressionSyntax)attributeParams[i].Expression);
                    var paramKeyword = paramType.Type;
                    if (paramKeyword is IdentifierNameSyntax paramIdentifierName)
                    {
                        attributeEntry.Params.Add(paramIdentifierName.Identifier.ValueText);
                    }
                    else if (paramKeyword is PredefinedTypeSyntax predefinedTypeSyntax)
                    {
                        attributeEntry.Params.Add(predefinedTypeSyntax.Keyword.ValueText);
                    }
                }

                entries.Add(attributeEntry);
            }

            return entries;
        }
    }
}
