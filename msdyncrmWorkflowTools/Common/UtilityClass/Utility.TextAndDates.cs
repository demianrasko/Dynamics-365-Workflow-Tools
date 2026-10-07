using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        // what the character after a backslash stands for in ExpandEscapes
        private static readonly Dictionary<string, string> Escapes = new Dictionary<string, string>
        {
            ["n"] = "\n",
            ["r"] = "\r",
            ["t"] = "\t",
            ["\\"] = "\\"
        };

        /// <summary>
        /// The difference between two dates and the parts of the first, for the Date Functions activity.
        /// </summary>
        public static DateFunctionsResult DateFunctions(DateTime date1, DateTime date2)
        {
            var difference = TimeSpan.Zero;
            var dayOfWeek = 0;
            var dayOfYear = 0;
            var day = 0;
            var month = 0;
            var year = 0;
            var weekOfYear = 0;

            DateFunctions(date1, date2, ref difference, ref dayOfWeek, ref dayOfYear, ref day, ref month, ref year, ref weekOfYear);

            return new DateFunctionsResult
            {
                Difference = difference,
                DayOfWeek = dayOfWeek,
                DayOfYear = dayOfYear,
                Day = day,
                Month = month,
                Year = year,
                WeekOfYear = weekOfYear
            };
        }

        public static bool DateFunctions(DateTime date1, DateTime date2, ref TimeSpan difference,
            ref int dayOfWeek, ref int dayOfYear, ref int day, ref int month, ref int year, ref int weekOfYear)
        {
            difference = date1 - date2;
            dayOfWeek = (int)date1.DayOfWeek;
            dayOfYear = date1.DayOfYear;
            day = date1.Day;
            month = date1.Month;
            year = date1.Year;

            var dateFormatInfo = DateTimeFormatInfo.CurrentInfo;
            var cal = dateFormatInfo.Calendar;

            weekOfYear = cal.GetWeekOfYear(date1, dateFormatInfo.CalendarWeekRule, dateFormatInfo.FirstDayOfWeek);

            return true;
        }

        /// <summary>
        /// Every String Functions output for one input text, for the String Functions activity.
        /// </summary>
        public static StringFunctionsResult StringFunctions(bool capitalizeAllWords, string inputText, string padCharacter, bool padOnTheLeft,
            int finalLengthWithPadding, bool caseSensitive, string replaceOldValue, string replaceNewValue,
            int subStringLength, int startIndex, bool fromLeftToRight, string regularExpression)
        {
            inputText = inputText ?? string.Empty;

            var capitalizedText = string.Empty;
            var paddedText = string.Empty;
            var replacedText = string.Empty;
            var subStringText = string.Empty;
            var regexText = string.Empty;
            var uppercaseText = string.Empty;
            var lowercaseText = string.Empty;
            var regexSuccess = false;
            var withoutSpaces = string.Empty;

            StringFunctions(capitalizeAllWords, inputText, padCharacter, padOnTheLeft, finalLengthWithPadding, caseSensitive,
                replaceOldValue, replaceNewValue ?? string.Empty, subStringLength, startIndex, fromLeftToRight, regularExpression,
                ref capitalizedText, ref paddedText, ref replacedText, ref subStringText, ref regexText,
                ref uppercaseText, ref lowercaseText, ref regexSuccess, ref withoutSpaces);

            return new StringFunctionsResult
            {
                CapitalizedText = capitalizedText,
                TextLength = capitalizedText.Length,
                PaddedText = paddedText,
                ReplacedText = replacedText,
                SubstringText = subStringText,
                TrimmedText = inputText.Trim(),
                RegexSuccess = regexSuccess,
                RegexText = regexText,
                UppercaseText = uppercaseText,
                LowercaseText = lowercaseText,
                WithoutSpaces = withoutSpaces,
                WithoutRegexMatches = RemoveRegexMatches(inputText, regularExpression)
            };
        }

        public static bool StringFunctions(bool capitalizeAllWords, string inputText, string padCharacter, bool padOnTheLeft,
            int finalLengthWithPadding, bool caseSensitive, string replaceOldValue, string replaceNewValue,
            int subStringLength, int startIndex, bool fromLeftToRight, string regularExpression,
            ref string capitalizedText, ref string paddedText, ref string replacedText, ref string subStringText, ref string regexText,
                ref string uppercaseText, ref string lowercaseText, ref bool regexSuccess, ref string withoutSpaces)
        {
            inputText = inputText ?? string.Empty;

            // capitalize all words, or the first letter only
            if (capitalizeAllWords)
            {
                capitalizedText = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(inputText);
            }
            else
            {
                capitalizedText = inputText.Length == 0 ? string.Empty : inputText.Substring(0, 1).ToUpper() + inputText.Substring(1);
            }

            // padding (a space when no pad character is given)
            var pad = string.IsNullOrEmpty(padCharacter) ? ' ' : padCharacter[0];
            paddedText = padOnTheLeft ? inputText.PadLeft(finalLengthWithPadding, pad) : inputText.PadRight(finalLengthWithPadding, pad);

            // replace
            // NOTE: "Case Sensitive" has always worked inverted (true ignores case, false matches case exactly).
            // It is kept that way so existing workflows behave the same.
            if (string.IsNullOrEmpty(replaceOldValue))
            {
                replacedText = inputText;
            }
            else if (caseSensitive)
            {
                replacedText = CompareAndReplace(inputText, replaceOldValue, replaceNewValue ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
            }
            else
            {
                replacedText = inputText.Replace(replaceOldValue, replaceNewValue ?? string.Empty);
            }

            // substring, cut short at the end of the text
            subStringText = string.Empty;
            if (subStringLength > 0 && startIndex >= 0)
            {
                if (!fromLeftToRight)
                {
                    startIndex = inputText.Length - subStringLength - startIndex;
                }

                if (startIndex < 0)
                {
                    subStringLength += startIndex;
                    startIndex = 0;
                }

                if (startIndex < inputText.Length && subStringLength > 0)
                {
                    subStringText = inputText.Substring(startIndex, Math.Min(subStringLength, inputText.Length - startIndex));
                }
            }

            // regex
            regexText = string.Empty;
            regexSuccess = false;
            if (!string.IsNullOrEmpty(regularExpression))
            {
                var match = new Regex(regularExpression).Match(inputText);

                if (match.Success)
                {
                    regexSuccess = true;
                    regexText = match.Value;
                }
            }

            uppercaseText = inputText.ToUpper();
            lowercaseText = inputText.ToLower();

            withoutSpaces = RemoveSpaces(inputText);

            return true;
        }

        /// <summary>
        /// The text with every match of a regular expression removed; the text unchanged when there's no expression.
        /// </summary>
        public static string RemoveRegexMatches(string text, string regularExpression)
        {
            text = text ?? string.Empty;

            return string.IsNullOrEmpty(regularExpression) ? text : new Regex(regularExpression).Replace(text, string.Empty);
        }

        /// <summary>
        /// The text without its spaces: the ordinary space and the other space characters, such as the non-breaking
        /// space a user's number format can put between digit groups. Tabs and line breaks are kept.
        /// </summary>
        public static string RemoveSpaces(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var result = new StringBuilder(text.Length);

            foreach (var character in text)
            {
                if (char.GetUnicodeCategory(character) != UnicodeCategory.SpaceSeparator)
                {
                    result.Append(character);
                }
            }

            return result.ToString();
        }

        private static string CompareAndReplace(string text, string old, string @new, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(old))
            {
                return text;
            }

            var result = new StringBuilder();
            var oldLength = old.Length;
            var pos = 0;
            var next = text.IndexOf(old, comparison);

            while (next >= 0)
            {
                result.Append(text, pos, next - pos);
                result.Append(@new);
                pos = next + oldLength;
                next = text.IndexOf(old, pos, comparison);
            }

            result.Append(text, pos, text.Length - pos);
            return result.ToString();
        }

        /// <summary>
        /// Turns the escapes \n, \r and \t typed into a workflow text input into a new line, carriage return and tab,
        /// since the workflow designer can't take those characters directly. "\\" stays a single backslash.
        /// </summary>
        public static string ExpandEscapes(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('\\') < 0)
            {
                return text;
            }

            return Regex.Replace(text, @"\\([\\nrt])", m => Escapes[m.Groups[1].Value]);
        }
    }
}
