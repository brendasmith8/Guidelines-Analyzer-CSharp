using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace CSharpGuidelinesAnalyzer.Rules.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotUseNumbersInIdentifiersAnalyzer : GuidelineAnalyzer
    {
        public const string DiagnosticId = "AV1704";

        private const string Title = "Identifier contains one or more digits in its name";
        private const string MessageFormat = "{0} '{1}' contains one or more digits in its name.";
        private const string Description = "Don't include numbers in variables, parameters and type members.";
        private const string Category = "Naming";

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Warning, true, Description, HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly ImmutableArray<SymbolKind> MemberSymbolKinds = ImmutableArray.Create(SymbolKind.Property,
            SymbolKind.Method, SymbolKind.Field, SymbolKind.Event);

        [NotNull]
        private static readonly char[] Digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(c => c.SkipEmptyName(AnalyzeNamedType), SymbolKind.NamedType);
            context.RegisterSymbolAction(c => c.SkipEmptyName(AnalyzeMember), MemberSymbolKinds);
            context.RegisterSyntaxNodeAction(c => c.SkipEmptyName(AnalyzeParameter), SyntaxKind.Parameter);

            context.RegisterConditionalOperationAction(c => c.SkipInvalid(AnalyzeVariableDeclaration),
                OperationKind.VariableDeclaration);
        }

        private void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (ContainsDigitsNonWhitelisted(type.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.TypeKind, type.Name));
            }
        }

        private void AnalyzeMember(SymbolAnalysisContext context)
        {
            ISymbol member = context.Symbol;

            if (member.IsPropertyOrEventAccessor() || context.Symbol.IsUnitTestMethod())
            {
                return;
            }

            if (ContainsDigitsNonWhitelisted(member.Name) && !member.IsOverride && !member.IsInterfaceImplementation())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, member.Locations[0], member.Kind, member.Name));
            }
        }

        private void AnalyzeParameter(SymbolAnalysisContext context)
        {
            var parameter = (IParameterSymbol)context.Symbol;

            if (ContainsDigitsNonWhitelisted(parameter.Name) && !parameter.ContainingSymbol.IsOverride &&
                !parameter.IsInterfaceImplementation())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Locations[0], parameter.Kind, parameter.Name));
            }
        }

        private void AnalyzeVariableDeclaration(OperationAnalysisContext context)
        {
            var declaration = (IVariableDeclaration)context.Operation;
            ILocalSymbol variable = declaration.Variables.Single();

            if (ContainsDigitsNonWhitelisted(variable.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, variable.Locations[0], "Variable", variable.Name));
            }
        }

        private static bool ContainsDigitsNonWhitelisted([NotNull] string text)
        {
            if (ContainsDigit(text))
            {
                string newText = RemoveWordsOnWhitelist(text);
                return ContainsDigit(newText);
            }

            return false;
        }

        [NotNull]
        private static string RemoveWordsOnWhitelist([NotNull] string text)
        {
            var tokenizer = new WordsTokenizer(text);
            List<WordToken> tokens = tokenizer.GetTokens().ToList();

            RemoveWhitelistedTokens(tokens);

            return string.Join(string.Empty, tokens.Select(t => t.Text));
        }

        private static void RemoveWhitelistedTokens([NotNull] List<WordToken> tokens)
        {
#pragma warning disable AV1530 // Loop variable should not be written to in loop body
            for (int index = 0; index < tokens.Count - 1; index++)
            {
                string thisTokenText = tokens[index].Text;
                string nextTokenText = tokens[index + 1].Text;

                if (IsSystemInt(thisTokenText, nextTokenText) || IsDimensional(thisTokenText, nextTokenText))
                {
                    tokens.RemoveRange(index, 2);
                    index--;
                }
            }
#pragma warning restore AV1530 // Loop variable should not be written to in loop body
        }

        private static bool IsDimensional([NotNull] string thisTokenText, [NotNull] string nextTokenText)
        {
            return (thisTokenText == "2" || thisTokenText == "3" || thisTokenText == "4") && nextTokenText == "D";
        }

        private static bool IsSystemInt([NotNull] string thisTokenText, [NotNull] string nextTokenText)
        {
            return thisTokenText == "Int" && (nextTokenText == "16" || nextTokenText == "32" || nextTokenText == "64");
        }

        private static bool ContainsDigit([NotNull] string text)
        {
            return text.IndexOfAny(Digits) != -1;
        }
    }
}
