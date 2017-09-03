using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpGuidelinesAnalyzer.Rules.Documentation
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DocumentAllInternalMembersAnalyzer : GuidelineAnalyzer
    {
        public const string DiagnosticId = "AV2305";

        private const string Title = "Missing XML comment for internally visible type, member or parameter";

        private const string MissingTypeOrMemberMessageFormat =
            "Missing XML comment for internally visible type or member '{0}'.";

        private const string MissingParameterMessageFormat = "Missing XML comment for internally visible parameter '{0}'.";
        private const string ExtraParameterMessageFormat = "Parameter '{0}' in XML comment not found in method signature.";
        private const string Description = "Document all public, protected and internal types and members.";
        private const string Category = "Documentation";

        [NotNull]
        private static readonly DiagnosticDescriptor MissingTypeOrMemberRule = new DiagnosticDescriptor(DiagnosticId, Title,
            MissingTypeOrMemberMessageFormat, Category, DiagnosticSeverity.Warning, true, Description,
            HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [NotNull]
        private static readonly DiagnosticDescriptor MissingParameterRule = new DiagnosticDescriptor(DiagnosticId, Title,
            MissingParameterMessageFormat, Category, DiagnosticSeverity.Warning, true, Description,
            HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [NotNull]
        private static readonly DiagnosticDescriptor ExtraParameterRule = new DiagnosticDescriptor(DiagnosticId, Title,
            ExtraParameterMessageFormat, Category, DiagnosticSeverity.Warning, true, Description,
            HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MissingTypeOrMemberRule, MissingParameterRule, ExtraParameterRule);

        private static readonly ImmutableArray<SymbolKind> MemberSymbolKinds =
            ImmutableArray.Create(SymbolKind.Property, SymbolKind.Method, SymbolKind.Field, SymbolKind.Event);

        [NotNull]
        [ItemNotNull]
        private static readonly HashSet<string> EmptyHashSet = new HashSet<string>();

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(c => c.SkipEmptyName(AnalyzeNamedType), SymbolKind.NamedType);
            context.RegisterSymbolAction(c => c.SkipEmptyName(AnalyzeMember), MemberSymbolKinds);
        }

        private void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            if (context.Symbol.AreDocumentationCommentsReported() && IsTypeAccessible(context.Symbol))
            {
                AnalyzeSymbol(context.Symbol, context);
            }
        }

        private static bool IsTypeAccessible([NotNull] ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Accessibility.Internal;
        }

        private void AnalyzeMember(SymbolAnalysisContext context)
        {
            if (context.Symbol.AreDocumentationCommentsReported() && !context.Symbol.IsPropertyOrEventAccessor() &&
                IsMemberAccessible(context.Symbol))
            {
                AnalyzeSymbol(context.Symbol, context);
            }
        }

        private static bool IsMemberAccessible([NotNull] ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Accessibility.Internal && symbol.IsSymbolAccessibleFromRoot();
        }

        private static void AnalyzeSymbol([NotNull] ISymbol symbol, SymbolAnalysisContext context)
        {
            string documentationXml = symbol.GetDocumentationCommentXml(null, false, context.CancellationToken);
            if (string.IsNullOrEmpty(documentationXml))
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingTypeOrMemberRule, symbol.Locations[0],
                    symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            if (symbol is IMethodSymbol method && method.Parameters.Any())
            {
                AnalyzeParameters(method.Parameters, documentationXml, context);
            }
        }

        private static void AnalyzeParameters([ItemNotNull] ImmutableArray<IParameterSymbol> parameters,
            [CanBeNull] string documentationXml, SymbolAnalysisContext context)
        {
            ISet<string> parameterNamesInDocumentation = string.IsNullOrEmpty(documentationXml)
                ? EmptyHashSet
                : TryParseDocumentationCommentXml(documentationXml);

            if (parameterNamesInDocumentation != null)
            {
                AnalyzeMissingParameters(parameters, parameterNamesInDocumentation, context);
                AnalyzeExtraParameters(parameterNamesInDocumentation, context);
            }
        }

        [CanBeNull]
        [ItemNotNull]
        private static ISet<string> TryParseDocumentationCommentXml([NotNull] string documentationXml)
        {
            try
            {
                XDocument document = XDocument.Parse(documentationXml);

                return GetParameterNamesFromXml(document);
            }
            catch (Exception)
            {
                return null;
            }
        }

        [NotNull]
        [ItemNotNull]
        private static ISet<string> GetParameterNamesFromXml([NotNull] XDocument document)
        {
            var parameterNames = new HashSet<string>();

            foreach (XElement paramElement in document.Element("member")?.Elements("param") ?? ImmutableArray<XElement>.Empty)
            {
                XAttribute paramAttribute = paramElement.Attribute("name");

                if (!string.IsNullOrEmpty(paramAttribute?.Value))
                {
                    parameterNames.Add(paramAttribute.Value);
                }
            }

            return parameterNames;
        }

        private static void AnalyzeMissingParameters([ItemNotNull] ImmutableArray<IParameterSymbol> parameters,
            [NotNull] [ItemNotNull] ISet<string> parameterNamesInDocumentation, SymbolAnalysisContext context)
        {
            foreach (IParameterSymbol parameter in parameters)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrEmpty(parameter.Name))
                {
                    if (!parameterNamesInDocumentation.Contains(parameter.Name))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(MissingParameterRule, parameter.Locations[0], parameter.Name));
                    }
                    else
                    {
                        parameterNamesInDocumentation.Remove(parameter.Name);
                    }
                }
            }
        }

        private static void AnalyzeExtraParameters([NotNull] [ItemNotNull] ISet<string> parameterNamesInDocumentation,
            SymbolAnalysisContext context)
        {
            foreach (string parameterNameInDocumentation in parameterNamesInDocumentation)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                context.ReportDiagnostic(Diagnostic.Create(ExtraParameterRule, context.Symbol.Locations[0],
                    parameterNameInDocumentation));
            }
        }
    }
}
