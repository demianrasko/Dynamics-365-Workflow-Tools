using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        public static bool DateFunctions(DateTime date1, DateTime date2, ref TimeSpan difference,
            ref int dayOfWeek, ref int dayOfYear, ref int day, ref int month, ref int year, ref int weekOfYear)
        {
            difference = date1 - date2;
            dayOfWeek = (int)date1.DayOfWeek;
            dayOfYear = date1.DayOfYear;
            day = date1.Day;
            month = date1.Month;
            year = date1.Year;
            var dfi = DateTimeFormatInfo.CurrentInfo;
            var cal = dfi.Calendar;

            weekOfYear = cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

            return true;
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

            withoutSpaces = inputText.Replace(" ", string.Empty);

            return true;
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
    }
}
