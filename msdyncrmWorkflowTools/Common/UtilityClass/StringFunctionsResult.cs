namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// The outputs of the String Functions activity (see <see cref="Utility.StringFunctions(bool, string, string, bool, int, bool, string, string, int, int, bool, string)"/>).
    /// </summary>
    public sealed class StringFunctionsResult
    {
        public string CapitalizedText { get; set; }

        /// <summary>The length of the capitalized text (the same as the input's).</summary>
        public int TextLength { get; set; }

        public string PaddedText { get; set; }

        public string ReplacedText { get; set; }

        public string SubstringText { get; set; }

        public string TrimmedText { get; set; }

        public bool RegexSuccess { get; set; }

        /// <summary>The first match of the regular expression.</summary>
        public string RegexText { get; set; }

        public string UppercaseText { get; set; }

        public string LowercaseText { get; set; }

        public string WithoutSpaces { get; set; }

        /// <summary>The input with every match of the regular expression removed.</summary>
        public string WithoutRegexMatches { get; set; }
    }
}
