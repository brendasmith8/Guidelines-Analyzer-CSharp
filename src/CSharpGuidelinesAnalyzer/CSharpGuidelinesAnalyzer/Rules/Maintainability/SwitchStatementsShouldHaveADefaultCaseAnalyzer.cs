using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace CSharpGuidelinesAnalyzer.Rules.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SwitchStatementsShouldHaveADefaultCaseAnalyzer : GuidelineAnalyzer
    {
        public const string DiagnosticId = "AV1536";

        private const string Title = "Incomplete switch statement without a default case clause";
        private const string MessageFormat = "Incomplete switch statement without a default case clause.";
        private const string Description = "Always add a default block after the last case in a switch statement.";
        private const string Category = "Maintainability";

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Warning, true, Description, HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        [NotNull]
        [ItemCanBeNull]
        private static readonly ISymbol[] NullSymbolArray = { null };

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(startContext =>
            {
                if (startContext.Compilation.SupportsOperations())
                {
                    INamedTypeSymbol systemBoolean = startContext.Compilation.GetTypeByMetadataName("System.Boolean");
                    if (systemBoolean != null)
                    {
                        startContext.RegisterOperationAction(c => c.SkipInvalid(_ => AnalyzeSwitchStatement(c, systemBoolean)),
                            OperationKind.SwitchStatement);
                    }
                }
            });
        }

        private void AnalyzeSwitchStatement(OperationAnalysisContext context, [NotNull] INamedTypeSymbol systemBoolean)
        {
            var switchStatement = (ISwitchStatement)context.Operation;

            if (HasDefaultCase(switchStatement))
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            var analysisContext = new SwitchAnalysisContext(switchStatement, systemBoolean, context);

            if (IsSwitchComplete(analysisContext) == false)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, switchStatement.Syntax.GetLocation()));
            }
        }

        private static bool HasDefaultCase([NotNull] ISwitchStatement switchStatement)
        {
            IEnumerable<ICaseClause> caseClauses = switchStatement.Cases.SelectMany(@case => @case.Clauses);
            return caseClauses.Any(clause => clause.CaseKind == CaseKind.Default);
        }

        [CanBeNull]
        private bool? IsSwitchComplete([NotNull] SwitchAnalysisContext analysisContext)
        {
            IdentifierInfo identifierInfo = analysisContext.SwitchStatement.Value.TryGetIdentifierInfo();

            return identifierInfo != null ? IsSwitchComplete(analysisContext, identifierInfo) : null;
        }

        [CanBeNull]
        private bool? IsSwitchComplete([NotNull] SwitchAnalysisContext analysisContext, [NotNull] IdentifierInfo identifierInfo)
        {
            return IsSwitchCompleteForBooleanTypes(identifierInfo, analysisContext) ??
                IsSwitchCompleteForEnumerationTypes(identifierInfo, analysisContext);
        }

        [CanBeNull]
        private bool? IsSwitchCompleteForBooleanTypes([NotNull] IdentifierInfo identifierInfo,
            [NotNull] SwitchAnalysisContext analysisContext)
        {
            bool isBoolean = identifierInfo.Type.SpecialType == SpecialType.System_Boolean;
            bool isNullableBoolean = identifierInfo.Type.IsNullableBoolean();

            if (isBoolean || isNullableBoolean)
            {
                ImmutableArray<ISymbol> possibleValues = isBoolean
                    ? ImmutableArray.Create(analysisContext.BooleanTrue, analysisContext.BooleanFalse)
                    : ImmutableArray.Create(analysisContext.BooleanTrue, analysisContext.BooleanFalse, null);

                return HasCaseClausesFor(possibleValues, analysisContext);
            }

            return null;
        }

        [CanBeNull]
        private bool? IsSwitchCompleteForEnumerationTypes([NotNull] IdentifierInfo identifierInfo,
            [NotNull] SwitchAnalysisContext analysisContext)
        {
            bool isEnumeration = identifierInfo.Type.BaseType != null &&
                identifierInfo.Type.BaseType.SpecialType == SpecialType.System_Enum;
            bool isNullableEnumeration = identifierInfo.Type.IsNullableEnumeration();

            if (isEnumeration || isNullableEnumeration)
            {
                ITypeSymbol enumType = isEnumeration
                    ? (INamedTypeSymbol)identifierInfo.Type
                    : ((INamedTypeSymbol)identifierInfo.Type).TypeArguments[0];

                ISymbol[] possibleValues = isEnumeration
                    ? enumType.GetMembers().OfType<IFieldSymbol>().Cast<ISymbol>().ToArray()
                    : enumType.GetMembers().OfType<IFieldSymbol>().Concat(NullSymbolArray).ToArray();

                return HasCaseClausesFor(possibleValues, analysisContext);
            }

            return null;
        }

        [CanBeNull]
        private bool? HasCaseClausesFor([NotNull] [ItemCanBeNull] ICollection<ISymbol> expectedValues,
            [NotNull] SwitchAnalysisContext analysisContext)
        {
            var collector = new CaseClauseCollector();
            ICollection<ISymbol> caseClauseValues = collector.TryGetSymbolsForCaseClauses(analysisContext);

            return caseClauseValues == null ? null : HasCaseClauseForExpectedValues(expectedValues, caseClauseValues);
        }

        [CanBeNull]
        private static bool? HasCaseClauseForExpectedValues([NotNull] [ItemCanBeNull] ICollection<ISymbol> expectedValues,
            [NotNull] [ItemCanBeNull] ICollection<ISymbol> caseClauseValues)
        {
            foreach (ISymbol expectedValue in expectedValues)
            {
                if (!caseClauseValues.Contains(expectedValue))
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class CaseClauseCollector
        {
            [NotNull]
            [ItemCanBeNull]
            private readonly HashSet<ISymbol> caseClauseValues = new HashSet<ISymbol>();

            [CanBeNull]
            [ItemCanBeNull]
            public ICollection<ISymbol> TryGetSymbolsForCaseClauses([NotNull] SwitchAnalysisContext analysisContext)
            {
                IEnumerable<ISingleValueCaseClause> caseClauses =
                    analysisContext.SwitchStatement.Cases.SelectMany(@case => @case.Clauses.OfType<ISingleValueCaseClause>());
                foreach (ISingleValueCaseClause caseClause in caseClauses)
                {
                    analysisContext.CancellationToken.ThrowIfCancellationRequested();

                    if (ProcessAsLiteralSyntax(analysisContext, caseClause) || ProcessAsField(caseClause) ||
                        ProcessAsConversion(analysisContext, caseClause))
                    {
                        continue;
                    }

#pragma warning disable AV2310 // Code blocks should not contain inline comments
                    // Switch statements with non-constant case expressions are not supported
                    // because they make completion analysis non-trivial.
#pragma warning restore AV2310 // Code blocks should not contain inline comments
                    return null;
                }

                return caseClauseValues;
            }

            private bool ProcessAsConversion([NotNull] SwitchAnalysisContext analysisContext,
                [NotNull] ISingleValueCaseClause caseClause)
            {
                var conversion = caseClause.Value as IConversionExpression;
                var memberSyntax = conversion?.Syntax as MemberAccessExpressionSyntax;

                IFieldSymbol field = analysisContext.GetFieldOrNull(memberSyntax);
                if (field != null)
                {
                    caseClauseValues.Add(field);
                    return true;
                }

                return false;
            }

            private bool ProcessAsLiteralSyntax([NotNull] SwitchAnalysisContext analysisContext,
                [NotNull] ISingleValueCaseClause caseClause)
            {
                if (caseClause.Value.Syntax is LiteralExpressionSyntax literalSyntax)
                {
                    if (ProcessLiteralSyntaxAsTrueKeyword(analysisContext, literalSyntax) ||
                        ProcessLiteralSyntaxAsFalseKeyword(analysisContext, literalSyntax) ||
                        ProcessLiteralSyntaxAsNullKeyword(literalSyntax))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool ProcessLiteralSyntaxAsTrueKeyword([NotNull] SwitchAnalysisContext analysisContext,
                [NotNull] LiteralExpressionSyntax literalSyntax)
            {
                if (literalSyntax.Token.IsKind(SyntaxKind.TrueKeyword))
                {
                    caseClauseValues.Add(analysisContext.BooleanTrue);
                    return true;
                }

                return false;
            }

            private bool ProcessLiteralSyntaxAsFalseKeyword([NotNull] SwitchAnalysisContext analysisContext,
                [NotNull] LiteralExpressionSyntax literalSyntax)
            {
                if (literalSyntax.Token.IsKind(SyntaxKind.FalseKeyword))
                {
                    caseClauseValues.Add(analysisContext.BooleanFalse);
                    return true;
                }
                return false;
            }

            private bool ProcessLiteralSyntaxAsNullKeyword([NotNull] LiteralExpressionSyntax literalSyntax)
            {
                if (literalSyntax.Token.IsKind(SyntaxKind.NullKeyword))
                {
                    caseClauseValues.Add(null);
                    return true;
                }
                return false;
            }

            private bool ProcessAsField([NotNull] ISingleValueCaseClause caseClause)
            {
                if (caseClause.Value is IFieldReferenceExpression enumField)
                {
                    caseClauseValues.Add(enumField.Field);
                    return true;
                }
                return false;
            }
        }

        private sealed class SwitchAnalysisContext
        {
            [NotNull]
            private readonly Compilation compilation;

            public CancellationToken CancellationToken { get; }

            [NotNull]
            public ISwitchStatement SwitchStatement { get; }

            [NotNull]
            public ISymbol BooleanTrue { get; }

            [NotNull]
            public ISymbol BooleanFalse { get; }

            public SwitchAnalysisContext([NotNull] ISwitchStatement switchStatement, [NotNull] INamedTypeSymbol systemBoolean,
                OperationAnalysisContext context)
            {
                Guard.NotNull(switchStatement, nameof(switchStatement));
                Guard.NotNull(systemBoolean, nameof(systemBoolean));

                SwitchStatement = switchStatement;
                compilation = context.Compilation;
                CancellationToken = context.CancellationToken;

                BooleanTrue = systemBoolean.GetMembers("TrueString").Single();
                BooleanFalse = systemBoolean.GetMembers("FalseString").Single();
            }

            [CanBeNull]
            public IFieldSymbol GetFieldOrNull([CanBeNull] MemberAccessExpressionSyntax memberSyntax)
            {
                if (memberSyntax != null)
                {
                    SemanticModel model = compilation.GetSemanticModel(memberSyntax.SyntaxTree);
                    return model.GetSymbolInfo(memberSyntax, CancellationToken).Symbol as IFieldSymbol;
                }

                return null;
            }
        }
    }
}
