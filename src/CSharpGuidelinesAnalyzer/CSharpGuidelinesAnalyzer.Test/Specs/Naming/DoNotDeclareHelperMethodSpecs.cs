using CSharpGuidelinesAnalyzer.Rules.Naming;
using CSharpGuidelinesAnalyzer.Test.TestDataBuilders;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CSharpGuidelinesAnalyzer.Test.Specs.Naming
{
#pragma warning disable AV1708 // Type name contains term that should be avoided
    public sealed class DoNotDeclareHelperMethodSpecs : CSharpGuidelinesAnalysisTestFixture
#pragma warning restore AV1708 // Type name contains term that should be avoided
    {
        protected override string DiagnosticId => DoNotDeclareHelperMethodAnalyzer.DiagnosticId;

        [Fact]
        internal void When_class_name_is_Helpers_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class [|Helpers|]
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Name of type 'Helpers' contains the term 'Helpers'.");
        }

        [Fact]
        internal void When_class_name_contains_the_word_Utility_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    static class [|StringUtility|]
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Name of type 'StringUtility' contains the term 'Utility'.");
        }

        [Fact]
        internal void When_struct_name_contains_the_word_Helpers_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    struct [|WebHelpers|]
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Name of type 'WebHelpers' contains the term 'Helpers'.");
        }

        [Fact]
        internal void When_enum_name_contains_the_word_Common_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    enum [|CommonState|]
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Name of type 'CommonState' contains the term 'Common'.");
        }

        [Fact]
        internal void When_class_name_contains_the_word_ShareDocument_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    class ShareDocument
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        internal void When_struct_name_contains_the_word_UncommonStory_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    struct UncommonStory
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        internal void When_interface_name_contains_the_word_Utility_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    interface [|IUtilityObjects|]
                    {
                    }
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Name of type 'IUtilityObjects' contains the term 'Utility'.");
        }

        [Fact]
        internal void When_delegate_name_contains_the_word_Facilities_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new TypeSourceCodeBuilder()
                .InGlobalScope(@"
                    delegate void [|ServiceFacilitiesUtilityCallback|]();
                ")
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Name of type 'ServiceFacilitiesUtilityCallback' contains the term 'Facilities'.");
        }

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            return new DoNotDeclareHelperMethodAnalyzer();
        }
    }
}
