using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        #endregion

        protected override void Execute(CodeActivityContext executionContext)
        {

            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            String inputText = this.InputText.Get(executionContext);
            bool capitalizeAllWords = this.CapitalizeAllWords.Get(executionContext);

            string padCharacter = this.PadCharacter.Get(executionContext);
            bool padontheLeft = this.PadontheLeft.Get(executionContext);
            int finalLengthwithPadding = this.FinalLengthwithPadding.Get(executionContext);

            string replaceOldValue = this.ReplaceOldValue.Get(executionContext);
            string replaceNewValue = this.ReplaceNewValue.Get(executionContext);
            if (replaceNewValue == null) replaceNewValue = "";
            bool caseSensitive = this.CaseSensitive.Get(executionContext);

            bool fromLefttoRight = this.FromLefttoRight.Get(executionContext);
            int startIndex = this.StartIndex.Get(executionContext);
            int subStringLength = this.SubStringLength.Get(executionContext);
            string regularExpression = this.RegularExpression.Get(executionContext);
            #endregion

            string capitalizedText = "";
            if (capitalizeAllWords)
            {
                // All words
                capitalizedText = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(inputText);
            }
            else
            {
                // First Letter only
                capitalizedText = inputText.Substring(0, 1).ToUpper() + inputText.Substring(1);
            }

            //padding
            string paddedText = "";
            if (padCharacter == "")
                padCharacter = " ";
            if (padontheLeft)
            {
                paddedText = inputText.PadLeft(finalLengthwithPadding, padCharacter.ToCharArray()[0]);
            }
            else
            {
                paddedText = inputText.PadRight(finalLengthwithPadding, padCharacter.ToCharArray()[0]);
            }

            //replace string
            string replacedText = "";
            if (!CaseSensitive.Get<bool>(executionContext))
            {
                if (!String.IsNullOrEmpty(inputText) && !String.IsNullOrEmpty(replaceOldValue))
                {
                    replacedText = inputText.Replace(replaceOldValue, replaceNewValue);
                }
            }
            else
            {
                replacedText = CompareAndReplace(inputText, replaceOldValue, replaceNewValue, StringComparison.CurrentCultureIgnoreCase);
            }

            //substring
            string subStringText = "";
            if (subStringLength <= 0 || startIndex< 0)
            {
                subStringText = String.Empty;
            }
            else
            {
                if (!fromLefttoRight)
                {
                    startIndex = inputText.Length - subStringLength - startIndex;
                }
                subStringText = inputText.Substring(startIndex, subStringLength);
            }

            //regex
            string regexText = "";
            bool regexSuccess = false;
            if (regularExpression != "")
            {
                Regex regex = new Regex(regularExpression);
                Match match = regex.Match(inputText);
                if (match.Success)
                {
                    regexSuccess = true;
                    regexText= match.Value;
                }

            }

            string uppercaseText = inputText.ToUpper();
            string lowercaseText = inputText.ToLower();

            this.CapitalizedText.Set(executionContext, capitalizedText);
            this.TextLength.Set(executionContext, capitalizedText.Length);
            this.PaddedText.Set(executionContext, paddedText);
            this.ReplacedText.Set(executionContext, replacedText);
            this.SubstringText.Set(executionContext, subStringText);
            this.TrimmedText.Set(executionContext, inputText.Trim());
            this.RegexSuccess.Set(executionContext, regexSuccess);
            this.RegexText.Set(executionContext, regexText);

            this.UppercaseText.Set(executionContext, uppercaseText);
            this.LowercaseText.Set(executionContext, lowercaseText);

        }
        private static string CompareAndReplace(string text, string old, string @new, StringComparison comparison)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(old)) return text;

            var result = new StringBuilder();
            var oldLength = old.Length;
            var pos = 0;
            var next = text.IndexOf(old, comparison);

            while (next > 0)
            {
                result.Append(text, pos, next - pos);
                result.Append(@new);
                pos = next + oldLength;
                next = text.IndexOf(old, pos, comparison);
            }

            result.Append(text, pos, text.Length - pos);
            return result.ToString();
        }

    }
}
