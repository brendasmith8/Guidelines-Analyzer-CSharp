using System.Collections.Immutable;
using System.Linq;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace CSharpGuidelinesAnalyzer.Rules.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CaseClausesInSwitchStatementsShouldHaveBracesAnalyzer : GuidelineAnalyzer
    {
        public const string DiagnosticId = "AV1535";

        private const string Title = "Missing block in case statement";
        private const string MessageFormat = "Missing block in case statement.";
        private const string Description = "Always add a block after keywords such as if, else, while, for, foreach and case.";
        private const string Category = "Maintainability";

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Warning, true, Description, HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterConditionalOperationAction(c => c.SkipInvalid(AnalyzeSwitchCase), OperationKind.SwitchCase);
        }

        private void AnalyzeSwitchCase(OperationAnalysisContext context)
        {
            var switchCase = (ISwitchCase)context.Operation;

            if (switchCase.Body.Length > 0)
            {
                if (!(switchCase.Body[0] is IBlockStatement))
                {
                    ReportAtLastClause(switchCase, context);
                }
            }
        }

        private static void ReportAtLastClause([NotNull] ISwitchCase switchCase, OperationAnalysisContext context)
        {
            ICaseClause lastClause = switchCase.Clauses.Last();

            Location location = lastClause.GetLocationForKeyword();
            context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
    }
}
