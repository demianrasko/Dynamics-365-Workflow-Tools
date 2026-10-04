using System;
using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools
{
    public class StringFunctions : CodeActivity
    {
        #region "Parameter Definition"
        [RequiredArgument]
        [Input("Input Text")]
        [Default("")]
        public InArgument<String> InputText { get; set; }

        [RequiredArgument]
        [Input("Capitalize All Words")]
        [Default("true")]
        public InArgument<bool> CapitalizeAllWords { get; set; }


        [RequiredArgument]
        [Input("Padding: Pad Character")]
        [Default("")]
        public InArgument<String> PadCharacter { get; set; }


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
        public InArgument<String> ReplaceOldValue { get; set; }

        [Input("Replace: New Value")]
        [Default("")]
        public InArgument<String> ReplaceNewValue { get; set; }

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
        public InArgument<int> SubStringLength{ get; set; }


        [RequiredArgument]
        [Input("Regular Expression")]
        [Default("")]
        public InArgument<String> RegularExpression { get; set; }


        [Output("Capitalized Text")]
        public OutArgument<String> CapitalizedText { get; set; }

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

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var inputText = InputText.Get(executionContext);
            if (inputText == null) inputText = "";
            var capitalizeAllWords = CapitalizeAllWords.Get(executionContext);

            var padCharacter = PadCharacter.Get(executionContext);
            var padontheLeft = PadontheLeft.Get(executionContext);
            var finalLengthwithPadding = FinalLengthwithPadding.Get(executionContext);

            var replaceOldValue = ReplaceOldValue.Get(executionContext);
            var replaceNewValue = ReplaceNewValue.Get(executionContext);
            if (replaceNewValue == null) replaceNewValue = "";
            var caseSensitive = CaseSensitive.Get(executionContext);

            var fromLefttoRight = FromLefttoRight.Get(executionContext);
            var startIndex = StartIndex.Get(executionContext);
            var subStringLength = SubStringLength.Get(executionContext);
            var regularExpression = RegularExpression.Get(executionContext);

            #endregion

            string capitalizedText="", paddedText = "", replacedText = "", subStringText = "", regexText = "", uppercaseText = "", lowercaseText="";
            var regexSuccess=false;
            var withoutSpaces = "";
            var commonClass = new msdyncrmWorkflowTools_Class(objCommon.service, objCommon.tracingService);
            var test=commonClass.StringFunctions(capitalizeAllWords, inputText, padCharacter, padontheLeft, finalLengthwithPadding, caseSensitive,
                replaceOldValue, replaceNewValue, subStringLength, startIndex, fromLefttoRight, regularExpression,
                ref capitalizedText, ref paddedText, ref replacedText, ref subStringText, ref regexText, 
                ref uppercaseText, ref lowercaseText, ref regexSuccess, ref withoutSpaces);
                
            

            CapitalizedText.Set(executionContext, capitalizedText);
            TextLength.Set(executionContext, capitalizedText.Length);
            PaddedText.Set(executionContext, paddedText);
            ReplacedText.Set(executionContext, replacedText);
            SubstringText.Set(executionContext, subStringText);
            TrimmedText.Set(executionContext, inputText.Trim());
            RegexSuccess.Set(executionContext, regexSuccess);
            RegexText.Set(executionContext, regexText);

            UppercaseText.Set(executionContext, uppercaseText);
            LowercaseText.Set(executionContext, lowercaseText);

            WithoutSpaces.Set(executionContext, withoutSpaces);

        }
        

    }
}
