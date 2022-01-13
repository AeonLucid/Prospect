using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prospect.Unreal.Generator.Util;
using Scriban;
using Scriban.Runtime;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Prospect.Unreal.Generator
{
    [Generator]
    internal class NetControlMessageSourceIncrementalGenerator : IIncrementalGenerator
    {
        public const string Namespace = "Prospect.Unreal.Net.Packets.Control";
        public const string AttributeName = "NetControlMessage";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var attributes = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: IsSyntaxTargetForGeneration, 
                    transform: GetSemanticTargetForGeneration)
                .Where(static m => m is not null);

            var compilationAndAttributes = context.CompilationProvider.Combine(attributes.Collect());

            context.RegisterSourceOutput(compilationAndAttributes, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken _) => 
            node is AttributeSyntax attribute && attribute.Name is IdentifierNameSyntax;

        private static AttributeSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken _)
        {
            var attribute = (AttributeSyntax)context.Node;
            var attributeName = (IdentifierNameSyntax)attribute.Name;

            if (attributeName.Identifier.ValueText != AttributeName)
            {
                return null;
            }

            return attribute;
        }

        private static void Execute(Compilation compilation, ImmutableArray<AttributeSyntax> attributes, SourceProductionContext context)
        {
            var parser = new NetControlMessageParser();
            var results = parser.Parse(attributes);
            
            if (results.Count == 0)
            {
                return;
            }

            context.AddSource("NMTEnum.g.cs", GenerateEnum(results));

            foreach (var result in results)
            {
                context.AddSource($"NMT_{result.Name}.g.cs", GenerateMessage(result));
            }
        }

        private static string GenerateEnum(IReadOnlyList<NetControlMessageEntry> entries)
        {
            var templateData = EmbeddedResource.GetContent("Templates/NMTEnum.sbntxt");
            var template = Template.Parse(templateData);

            return template.Render(new
            {
                cnamespace = Namespace,
                entries = entries
            }, member => member.Name).Trim();
        }

        private static string GenerateMessage(NetControlMessageEntry entry)
        {
            var scriptObject = new ScriptObject();

            scriptObject.Import(typeof(ScribanFunctions));
            scriptObject.Add("cnamespace", Namespace);
            scriptObject.Add("entry", entry);

            var context = new TemplateContext();

            context.PushGlobal(scriptObject);

            var templateData = EmbeddedResource.GetContent("Templates/NMTMessage.sbntxt");
            var template = Template.Parse(templateData);

            return template.Render(scriptObject, member => member.Name).Trim();
        }
    }

    internal static class ScribanFunctions
    {
        [ScriptMemberIgnore]
        private static readonly Dictionary<string, ParamDef> TypeDefinitions = new Dictionary<string, ParamDef>
        {
            { "byte", new ParamDef("bunch.ReadByte()", "bunch.WriteByte({0})") },
            { "int", new ParamDef("bunch.ReadInt32()", "bunch.WriteInt32({0})") },
            { "uint", new ParamDef("bunch.ReadUInt32()", "bunch.WriteUInt32({0})") },
            { "FString", new ParamDef("bunch.ReadString()", "bunch.WriteString({0})") },
            { "FUniqueNetIdRepl", new ParamDef("FUniqueNetIdRepl.Read(bunch)", "FUniqueNetIdRepl.Write(bunch, {0})") }
        };

        public static string SendParams(List<string> args)
        {
            return MethodParameter(args);
        }

        public static string ReadOutParams(List<string> args)
        {
            return MethodParameter(args, "out");
        }

        public static string ReadOut(string paramType, int index)
        {
            var paramName = (char)('a' + index);
            var paramRead = ReadType(paramType);
            if (paramRead.StartsWith("throw")) {
                return paramRead;
            }

            return $"{paramName} = {paramRead}";
        }

        public static string SendType(string paramType, int index)
        {
            if (TypeDefinitions.TryGetValue(paramType, out var type))
            {
                return string.Format(type.Write, (char)('a' + index));
            }

            return $"throw new NotImplementedException(\"Unsupported type {paramType}\")";
        }
        
        public static string ReadType(string param)
        {
            if (TypeDefinitions.TryGetValue(param, out var type))
            {
                return type.Read;
            }

            return $"throw new NotImplementedException(\"Unsupported type {param}\")";
        }

        [ScriptMemberIgnore]
        private static string MethodParameter(List<string> args, string prefix = null)
        {

            var builder = new StringBuilder();

            for (int i = 0; i < args.Count; i++)
            {
                if (i < args.Count)
                {
                    builder.Append(", ");
                }

                var paramName = (char)('a' + i);
                var paramType = args[i];
                if (paramType == "FString")
                {
                    paramType = "string";
                }

                if (prefix != null)
                {
                    builder.AppendFormat("{0} {1} {2}", prefix, paramType, paramName);
                }
                else
                {
                    builder.AppendFormat("{0} {1}", paramType, paramName);
                }
            }

            return builder.ToString();
        }
    }
}