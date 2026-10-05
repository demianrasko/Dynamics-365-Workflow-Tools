using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        /// <summary>
        /// A numeric attribute value (number, Money or an AliasedValue wrapping one) as a decimal; null for
        /// anything else, including a missing value.
        /// </summary>
        public static decimal? ToDecimal(object value)
        {
            while (true)
            {
                switch (value)
                {
                    case AliasedValue aliasedValue:
                        value = aliasedValue.Value;
                        continue;
                    case Money money:
                        return money.Value;
                    case decimal _:
                    case int _:
                    case long _:
                    case short _:
                    case float _:
                    case double _:
                        return Convert.ToDecimal(value);
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Converts a Dataverse attribute value to the string a workflow output expects.
        /// </summary>
        /// <remarks>
        /// OptionSetValue returns the number, EntityReference the record id, Money the amount,
        /// OptionSetValueCollection the numbers separated by commas, and AliasedValue the value it wraps.
        /// Everything else uses its own ToString(), so existing outputs (dates, numbers, text) are unchanged.
        /// </remarks>
        /// <param name="value">The attribute value, e.g. entity.Attributes["name"]. Null returns null.</param>
        public static string AttributeValueToString(object value)
        {
            while (true)
            {
                switch (value)
                {
                    case null:
                        return null;
                    case AliasedValue aliasedValue:
                        value = aliasedValue.Value;
                        continue;
                    case OptionSetValue optionSetValue:
                        return optionSetValue.Value.ToString();
                    case OptionSetValueCollection optionSetValues:
                        return string.Join(",", optionSetValues.Select(o => o.Value));
                    case EntityReference entityReference:
                        return entityReference.Id.ToString();
                    case Money money:
                        return money.Value.ToString(CultureInfo.InvariantCulture);
                    default:
                        return value.ToString();
                }
            }
        }

        /// <summary>
        /// Copies an attribute value from one record to another when the source record has it.
        /// A missing source attribute leaves the target unchanged.
        /// </summary>
        /// <param name="source">Record to read from.</param>
        /// <param name="sourceAttribute">Logical name of the attribute to read.</param>
        /// <param name="target">Record to write to.</param>
        /// <param name="targetAttribute">Logical name of the attribute to set; defaults to <paramref name="sourceAttribute"/>.</param>
        /// <returns>True when the value was copied.</returns>
        public static bool CopyAttributeValue(Entity source, string sourceAttribute, Entity target, string targetAttribute = null)
        {
            if (!source.TryGetAttributeValue(sourceAttribute, out object value))
            {
                return false;
            }

            target[targetAttribute ?? sourceAttribute] = value;

            return true;
        }

        /// <summary>
        /// Parses a comma-separated list of option values (e.g. "1,3,7") into an OptionSetValueCollection.
        /// Values that are not whole numbers are skipped and added to <paramref name="invalidValues"/> when given.
        /// </summary>
        /// <param name="values">The comma-separated values. Null or empty returns an empty collection.</param>
        /// <param name="invalidValues">Optional list that receives the values that could not be parsed.</param>
        public static OptionSetValueCollection ParseOptionSetValues(string values, ICollection<string> invalidValues = null)
        {
            var collection = new OptionSetValueCollection();

            if (string.IsNullOrEmpty(values))
            {
                return collection;
            }

            foreach (var value in values.Split(','))
            {
                if (int.TryParse(value, out var number))
                {
                    collection.Add(new OptionSetValue(number));
                }
                else
                {
                    invalidValues?.Add(value);
                }
            }

            return collection;
        }

        /// <summary>
        /// The values of <paramref name="existingValues"/> followed by the values of <paramref name="newValues"/> it
        /// does not already contain. Either collection may be null.
        /// </summary>
        public static OptionSetValueCollection MergeOptionSetValues(OptionSetValueCollection newValues, OptionSetValueCollection existingValues)
        {
            var merged = new OptionSetValueCollection();

            foreach (var value in (existingValues ?? new OptionSetValueCollection()).Concat(newValues ?? new OptionSetValueCollection()))
            {
                if (!merged.Any(v => v.Value == value.Value))
                {
                    merged.Add(value);
                }
            }

            return merged;
        }

        /// <summary>
        /// Option set values as a comma-separated list of numbers, e.g. "1,3,7".
        /// </summary>
        public static string JoinOptionSetValues(IEnumerable<OptionSetValue> values)
        {
            return string.Join(",", values.Select(v => v.Value));
        }

        /// <summary>
        /// Option set values as a comma-separated list of labels; a value without a label is written as its number.
        /// </summary>
        public static string JoinOptionSetLabels(IEnumerable<OptionSetValue> values, IDictionary<int, string> labels)
        {
            return string.Join(",", values.Select(v => labels.TryGetValue(v.Value, out var label) ? label : v.Value.ToString()));
        }

        /// <summary>
        /// Splits a comma-separated list of field logical names, trimming spaces and dropping empty entries.
        /// </summary>
        public static string[] SplitAttributeNames(string attributeNames)
        {
            return (attributeNames ?? string.Empty)
                .Split(',')
                .Select(a => a.Trim())
                .Where(a => a.Length > 0)
                .ToArray();
        }

        /// <summary>
        /// The typed value to store in an organization setting: a whole number, true/false, or the text itself.
        /// </summary>
        public static object ConvertSettingValue(string value)
        {
            if (int.TryParse(value, out var number))
            {
                return number;
            }

            if (bool.TryParse(value, out var flag))
            {
                return flag;
            }

            return value;
        }

        /// <summary>The records-per-page values Dataverse accepts for usersettings.paginglimit.</summary>
        private static readonly int[] ValidPagingLimits = { 25, 50, 75, 100, 250 };

        /// <summary>
        /// The usersettings update for SetUserSettings, holding only the settings that were supplied:
        /// 0 leaves a number unchanged, as does an AdvancedFind mode other than 1 or 2 and a calendar view
        /// other than 0, 1 or 2. A null Send As leaves it unchanged.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The paging limit is not 0, 25, 50, 75, 100 or 250.</exception>
        public static Entity BuildUserSettings(Guid userId, int pagingLimit, int advancedFindStartupMode, int timeZoneCode,
            int helpLanguageId, int uiLanguageId, int defaultCalendarView, bool? isSendAsAllowed)
        {
            var settings = new Entity(EntityNames.UserSettings)
            {
                [AttributeNames.SystemUserId] = userId
            };

            if (pagingLimit != 0)
            {
                if (!ValidPagingLimits.Contains(pagingLimit))
                {
                    throw new InvalidPluginExecutionException(
                        $"PagingLimit must be 25, 50, 75, 100 or 250 (or 0 to leave it unchanged), not {pagingLimit}.");
                }

                settings[AttributeNames.PagingLimit] = pagingLimit;
            }

            if (advancedFindStartupMode == 1 || advancedFindStartupMode == 2)
            {
                settings[AttributeNames.AdvancedFindStartupMode] = advancedFindStartupMode;
            }

            if (timeZoneCode != 0)
            {
                settings[AttributeNames.TimeZoneCode] = timeZoneCode;
            }

            if (helpLanguageId != 0)
            {
                settings[AttributeNames.HelpLanguageId] = helpLanguageId;
            }

            if (uiLanguageId != 0)
            {
                settings[AttributeNames.UILanguageId] = uiLanguageId;
            }

            if (defaultCalendarView >= 0 && defaultCalendarView <= 2)
            {
                settings[AttributeNames.DefaultCalendarView] = defaultCalendarView;
            }

            if (isSendAsAllowed.HasValue)
            {
                settings[AttributeNames.IsSendAsAllowed] = isSendAsAllowed.Value;
            }

            return settings;
        }

        /// <summary>
        /// Splits a comma-separated category list for AIClassify: trimmed, no empty entries, no duplicates
        /// (ignoring case), in the original order.
        /// </summary>
        public static List<string> ParseCategories(string categories)
        {
            return (categories ?? string.Empty)
                .Split(',')
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The marketing list member a workflow step was given: the account, else the contact, else the lead.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">None of them is set.</exception>
        public static EntityReference GetMarketingListMember(EntityReference account, EntityReference contact, EntityReference lead)
        {
            return account ?? contact ?? lead
                ?? throw new InvalidPluginExecutionException("Account, Contact or Lead is required.");
        }
    }
}
