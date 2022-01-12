using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Prospect.Unreal.Generator
{
    internal class NetControlMessageSyntaxReceiver : ISyntaxContextReceiver
    {
        public const string AttributeName = "NetControlMessage";

        public List<NetControlMessageEntry> Entries { get; } = new List<NetControlMessageEntry>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not AttributeSyntax attribute)
            {
                return;
            }

            if (attribute.Name is not IdentifierNameSyntax identifierName)
            {
                return;
            }

            if (identifierName.Identifier.ValueText != AttributeName)
            {
                return;
            }

            var attributeParams = attribute.ArgumentList.Arguments;
            var attributeEntry = new NetControlMessageEntry();

            attributeEntry.Name = ((LiteralExpressionSyntax)attributeParams[0].Expression).Token.ValueText; // StringLiteralExpression
            attributeEntry.Index = (int) ((LiteralExpressionSyntax)attributeParams[1].Expression).Token.Value; // NumericLiteralExpression

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

            Entries.Add(attributeEntry);
        }
    }
}