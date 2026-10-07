using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("String Functions")]
    public class StringFunctions : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Input Text")]
        [Default("")]
        public InArgument<string> InputText { get; set; }

        [RequiredArgument]
        [Input("Capitalize All Words")]
        [Default("true")]
        public InArgument<bool> CapitalizeAllWords { get; set; }

        [RequiredArgument]
        [Input("Padding: Pad Character")]
        [Default("")]
        public InArgument<string> PadCharacter { get; set; }

        [RequiredArgument]
        [Input("Padding: Pad on the Left")]
        [Default("false")]
        public InArgument<bool> PadontheLeft { get; set; }

        [RequiredArgument]
        [Input("Padding: Final Length")]
        [Default("10")]
        public InArgument<int> FinalLengthwithPadding { get; set; }

        [RequiredArgument]
        [Input("Replace: Old Value")]
        [Default("")]
        public InArgument<string> ReplaceOldValue { get; set; }

        [Input("Replace: New Value")]
        [Default("")]
        public InArgument<string> ReplaceNewValue { get; set; }

        [RequiredArgument]
        [Input("Replace: Case Sensitive")]
        [Default("false")]
        public InArgument<bool> CaseSensitive { get; set; }

        [RequiredArgument]
        [Input("Substring: From Left to Right")]
        [Default("true")]
        public InArgument<bool> FromLefttoRight { get; set; }

        [RequiredArgument]
        [Input("Substring: Start Index")]
        [Default("0")]
        public InArgument<int> StartIndex { get; set; }

        [RequiredArgument]
        [Input("Substring: Length")]
        [Default("3")]
        public InArgument<int> SubStringLength { get; set; }

        [RequiredArgument]
        [Input("Regular Expression")]
        [Default("")]
        public InArgument<string> RegularExpression { get; set; }

        [Output("Capitalized Text")]
        public OutArgument<string> CapitalizedText { get; set; }

        [Output("Text Length")]
        public OutArgument<int> TextLength { get; set; }

        [Output("Padded Text")]
        public OutArgument<string> PaddedText { get; set; }

        [Output("Replaced Text")]
        public OutArgument<string> ReplacedText { get; set; }

        [Output("Substring Text")]
        public OutArgument<string> SubstringText { get; set; }

        [Output("Trimmed Text")]
        public OutArgument<string> TrimmedText { get; set; }

        [Output("Regex Success")]
        public OutArgument<bool> RegexSuccess { get; set; }

        [Output("Regex Text")]
        public OutArgument<string> RegexText { get; set; }

        [Output("Uppercase Text")]
        public OutArgument<string> UppercaseText { get; set; }

        [Output("Lowercase Text")]
        public OutArgument<string> LowercaseText { get; set; }

        [Output("Without Spaces")]
        public OutArgument<string> WithoutSpaces { get; set; }

        [Output("Without Regex Matches")]
        public OutArgument<string> WithoutRegexMatches { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var result = Utility.StringFunctions(CapitalizeAllWords.Get(executionContext), InputText.Get(executionContext), PadCharacter.Get(executionContext),
                PadontheLeft.Get(executionContext), FinalLengthwithPadding.Get(executionContext), CaseSensitive.Get(executionContext),
                ReplaceOldValue.Get(executionContext), ReplaceNewValue.Get(executionContext), SubStringLength.Get(executionContext),
                StartIndex.Get(executionContext), FromLefttoRight.Get(executionContext), RegularExpression.Get(executionContext));

            CapitalizedText.Set(executionContext, result.CapitalizedText);
            TextLength.Set(executionContext, result.TextLength);
            PaddedText.Set(executionContext, result.PaddedText);
            ReplacedText.Set(executionContext, result.ReplacedText);
            SubstringText.Set(executionContext, result.SubstringText);
            TrimmedText.Set(executionContext, result.TrimmedText);
            RegexSuccess.Set(executionContext, result.RegexSuccess);
            RegexText.Set(executionContext, result.RegexText);
            UppercaseText.Set(executionContext, result.UppercaseText);
            LowercaseText.Set(executionContext, result.LowercaseText);
            WithoutSpaces.Set(executionContext, result.WithoutSpaces);
            WithoutRegexMatches.Set(executionContext, result.WithoutRegexMatches);
        }
    }
}
