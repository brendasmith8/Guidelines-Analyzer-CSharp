using CSharpGuidelinesAnalyzer.Rules.Naming;
using CSharpGuidelinesAnalyzer.Test.TestDataBuilders;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CSharpGuidelinesAnalyzer.Test.Specs.Naming
{
    public sealed class StaticClassesShouldOnlyContainExtensionMethodsSpecs : CSharpGuidelinesAnalysisTestFixture
    {
        protected override string DiagnosticId => StaticClassesShouldOnlyContainExtensionMethodsAnalyzer.DiagnosticId;

        [Fact]
        internal void When_class_is_not_static_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class Container
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        internal void When_static_class_has_type_parameters_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    static class Container<T>
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        internal void When_misnamed_static_class_has_public_extension_method_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    public static class [|Container|]
                    {
                        public static void M(this string s)
                        {
                        }

                        private static void InnerMethod(string s)
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Name of extension method container class 'Container' should end with 'Extensions'.");
        }

        [Fact]
        internal void When_misnamed_static_class_has_internal_extension_method_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    public static class [|Container|]
                    {
                        internal static void M(this string s)
                        {
                        }

                        private static void InnerMethod(string s)
                        {
                        }
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Name of extension method container class 'Container' should end with 'Extensions'.");
        }

        [Fact]
        internal void When_misnamed_static_class_has_no_extension_methods_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    public static class Container
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            return new StaticClassesShouldOnlyContainExtensionMethodsAnalyzer();
        }
    }
}