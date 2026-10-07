using Microsoft.VisualStudio.TestTools.UnitTesting;
using msdyncrmWorkflowTools;

namespace msdyncrmWorkflowTools_Tests
{
    [TestClass]
    public class StringFunctions_Tests
    {
        [TestMethod]
        public void StringFunctions1()
        {
            string capitalizedText = string.Empty, paddedText = string.Empty, replacedText = string.Empty, subStringText = string.Empty, regexText = string.Empty, uppercaseText = string.Empty, lowercaseText = string.Empty;
            var regexSuccess = false;
            var withoutSpaces = string.Empty;

            var test = Utility.StringFunctions(true, "Demian", "w", true, 150, true,
                "w", "w", 150, 0, true, "w",
                ref capitalizedText, ref paddedText, ref replacedText, ref subStringText, ref regexText,
                ref uppercaseText, ref lowercaseText, ref regexSuccess, ref withoutSpaces);

            Assert.AreEqual(capitalizedText, "Demian");
            Assert.AreEqual(paddedText, "wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwDemian");
            Assert.AreEqual(replacedText, "Demian");
            Assert.AreEqual(subStringText, "Demian");
            Assert.AreEqual(regexText, string.Empty);
            Assert.AreEqual(uppercaseText, "DEMIAN");
            Assert.AreEqual(lowercaseText, "demian");
            Assert.AreEqual(regexSuccess, false);
        }
        [TestMethod]
        public void StringFunctions2()
        {
            string capitalizedText = string.Empty, paddedText = string.Empty, replacedText = string.Empty, subStringText = string.Empty, regexText = string.Empty, uppercaseText = string.Empty, lowercaseText = string.Empty;
            var regexSuccess = false;
            var withoutSpaces = string.Empty;

            var test = Utility.StringFunctions(true, "Demian", "w", true, 10, true,
                "w", "w", 150, 0, true, "w",
                ref capitalizedText, ref paddedText, ref replacedText, ref subStringText, ref regexText,
                ref uppercaseText, ref lowercaseText, ref regexSuccess, ref withoutSpaces);

            Assert.AreEqual(capitalizedText, "Demian");
            Assert.AreEqual(paddedText, "wwwwDemian");
            Assert.AreEqual(replacedText, "Demian");
            Assert.AreEqual(subStringText, "Demian");
            Assert.AreEqual(regexText, string.Empty);
            Assert.AreEqual(uppercaseText, "DEMIAN");
            Assert.AreEqual(lowercaseText, "demian");
            Assert.AreEqual(regexSuccess, false);
        }

        private sealed class Result
        {
            public string Capitalized, Padded, Replaced, SubString, Regex, Upper, Lower, WithoutSpaces;
            public bool RegexSuccess;
        }

        private static Result Run(string input, bool capitalizeAllWords = true, string padCharacter = "", bool padOnTheLeft = false,
            int finalLength = 10, bool caseSensitive = false, string oldValue = "", string newValue = "",
            int subStringLength = 3, int startIndex = 0, bool fromLeftToRight = true, string regularExpression = "")
        {
            var r = new Result();
            Utility.StringFunctions(capitalizeAllWords, input, padCharacter, padOnTheLeft, finalLength, caseSensitive, oldValue, newValue,
                subStringLength, startIndex, fromLeftToRight, regularExpression,
                ref r.Capitalized, ref r.Padded, ref r.Replaced, ref r.SubString, ref r.Regex,
                ref r.Upper, ref r.Lower, ref r.RegexSuccess, ref r.WithoutSpaces);

            return r;
        }

        [TestMethod]
        public void Capitalize_AllWordsOrFirstLetterOnly()
        {
            Assert.AreEqual("Hello World", Run("hello world").Capitalized);
            Assert.AreEqual("Hello world", Run("hello world", capitalizeAllWords: false).Capitalized);
        }

        [TestMethod]
        public void Pad_RightWithSpacesByDefaultOrLeftWithACharacter()
        {
            Assert.AreEqual("abc   ", Run("abc", finalLength: 6).Padded);
            Assert.AreEqual("000abc", Run("abc", padCharacter: "0", padOnTheLeft: true, finalLength: 6).Padded);
            Assert.AreEqual("abcdef", Run("abcdef", finalLength: 3).Padded);
        }

        // "Case Sensitive" is inverted on purpose (kept for existing workflows): true ignores case, false matches case exactly.
        [TestMethod]
        public void Replace_CaseSensitiveTrueIgnoresCase()
        {
            Assert.AreEqual("x b x", Run("a b A", caseSensitive: true, oldValue: "a", newValue: "x").Replaced);
        }

        [TestMethod]
        public void Replace_CaseSensitiveFalseMatchesTheSameCaseOnly()
        {
            Assert.AreEqual("x b A", Run("a b A", caseSensitive: false, oldValue: "a", newValue: "x").Replaced);
        }

        [TestMethod]
        public void Replace_MatchAtTheStartIsReplaced()
        {
            Assert.AreEqual("zzbczzbc", Run("abcabc", caseSensitive: true, oldValue: "a", newValue: "zz").Replaced);
        }

        [TestMethod]
        public void Replace_NoOldValueKeepsTheText()
        {
            Assert.AreEqual("abc", Run("abc", caseSensitive: true).Replaced);
            Assert.AreEqual("abc", Run("abc", caseSensitive: false).Replaced);
        }

        [TestMethod]
        public void SubString_FromTheLeftOrTheRight()
        {
            Assert.AreEqual("bcd", Run("abcdef", subStringLength: 3, startIndex: 1).SubString);
            Assert.AreEqual("cde", Run("abcdef", subStringLength: 3, startIndex: 1, fromLeftToRight: false).SubString);
        }

        [TestMethod]
        public void SubString_PastTheEndIsCutShortInsteadOfThrowing()
        {
            Assert.AreEqual("ef", Run("abcdef", subStringLength: 5, startIndex: 4).SubString);
            Assert.AreEqual(string.Empty, Run("abc", subStringLength: 2, startIndex: 10).SubString);
            Assert.AreEqual("abc", Run("abc", subStringLength: 10, startIndex: 0, fromLeftToRight: false).SubString);
        }

        [TestMethod]
        public void SubString_ZeroLengthOrNegativeStartIsEmpty()
        {
            Assert.AreEqual(string.Empty, Run("abc", subStringLength: 0).SubString);
            Assert.AreEqual(string.Empty, Run("abc", startIndex: -1).SubString);
        }

        [TestMethod]
        public void Regex_ReturnsTheFirstMatch()
        {
            var r = Run("Order 12345 shipped", regularExpression: @"\d+");

            Assert.IsTrue(r.RegexSuccess);
            Assert.AreEqual("12345", r.Regex);
            Assert.IsFalse(Run("no digits", regularExpression: @"\d+").RegexSuccess);
        }

        [TestMethod]
        public void CaseAndSpaces()
        {
            var r = Run("Mixed Case Text");

            Assert.AreEqual("MIXED CASE TEXT", r.Upper);
            Assert.AreEqual("mixed case text", r.Lower);
            Assert.AreEqual("MixedCaseText", r.WithoutSpaces);
        }

        [TestMethod]
        public void RemoveRegexMatches_RemovesEveryMatch()
        {
            // upstream PR #285
            Assert.AreEqual("abc", Utility.RemoveRegexMatches("a1b22c333", @"\d+"));
            Assert.AreEqual("a1b2", Utility.RemoveRegexMatches("a1b2", null), "no expression leaves the text as it is");
            Assert.AreEqual(string.Empty, Utility.RemoveRegexMatches(null, @"\d"));
        }

        [TestMethod]
        public void WithoutSpacesRemovesTheSpacesANumberFormatUses()
        {
            // a non-breaking space and a narrow non-breaking space between digit groups (upstream issue #267)
            Assert.AreEqual("ABCD1000002", Run("ABCD1 000 002").WithoutSpaces);
            Assert.AreEqual("a\tb\nc", Run("a\t b\n c").WithoutSpaces, "tabs and line breaks are kept");
        }

        [TestMethod]
        public void NullInputsDoNotThrow()
        {
            var r = Run(null, capitalizeAllWords: false, padCharacter: null, oldValue: null, newValue: null, regularExpression: null);

            Assert.AreEqual(string.Empty, r.Capitalized);
            Assert.AreEqual(new string(' ', 10), r.Padded);
            Assert.AreEqual(string.Empty, r.Replaced);
            Assert.AreEqual(string.Empty, r.SubString);
            Assert.IsFalse(r.RegexSuccess);
            Assert.AreEqual(string.Empty, r.Upper);
            Assert.AreEqual(string.Empty, r.WithoutSpaces);
        }

        [TestMethod]
        public void StringFunctions_ResultHasEveryOutput()
        {
            var result = Utility.StringFunctions(false, "  ab1 c2  ", "*", true, 12, false, "c", null, 2, 2, true, @"\d");

            Assert.AreEqual("  ab1 c2  ", result.CapitalizedText);
            Assert.AreEqual(10, result.TextLength);
            Assert.AreEqual("**  ab1 c2  ", result.PaddedText);
            Assert.AreEqual("  ab1 2  ", result.ReplacedText, "an empty new value removes the old one");
            Assert.AreEqual("ab", result.SubstringText);
            Assert.AreEqual("ab1 c2", result.TrimmedText);
            Assert.IsTrue(result.RegexSuccess);
            Assert.AreEqual("1", result.RegexText);
            Assert.AreEqual("  AB1 C2  ", result.UppercaseText);
            Assert.AreEqual("  ab1 c2  ", result.LowercaseText);
            Assert.AreEqual("ab1c2", result.WithoutSpaces);
            Assert.AreEqual("  ab c  ", result.WithoutRegexMatches);
        }

        [TestMethod]
        public void StringFunctions_NullInputIsEmpty()
        {
            var result = Utility.StringFunctions(true, null, null, false, 0, false, null, null, 0, 0, true, null);

            Assert.AreEqual(string.Empty, result.TrimmedText);
            Assert.AreEqual(0, result.TextLength);
            Assert.AreEqual(string.Empty, result.WithoutRegexMatches);
        }
    }
}
