using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        /// <summary>
        /// Whether a fetch query has a top attribute (which cannot be combined with paging).
        /// </summary>
        public static bool HasFetchTop(string fetchXml)
        {
            try
            {
                return System.Xml.Linq.XElement.Parse(fetchXml).Attribute("top") != null;
            }
            catch (XmlException)
            {
                return false;
            }
        }

        /// <summary>
        /// One record's value for ConcatenateFromQuery: the named attribute (or the first attribute when no name is
        /// given), unwrapped from AliasedValue; a lookup's name, a Money amount, a choice's label (its number when no
        /// label was returned); then formatted with <paramref name="format"/>.
        /// </summary>
        /// <returns>The formatted value, or null when the record has no value.</returns>
        public static string FormatConcatenationValue(Entity record, string attributeName, string format)
        {
            object value;

            if (string.IsNullOrEmpty(attributeName))
            {
                value = record.Attributes.Count > 0 ? record.Attributes.First().Value : null;
            }
            else
            {
                value = record.Contains(attributeName) ? record[attributeName] : null;
            }

            if (value is AliasedValue aliasedValue)
            {
                value = aliasedValue.Value;
            }

            switch (value)
            {
                case null:
                    return null;
                case EntityReference reference:
                    value = reference.Name;
                    break;
                case Money money:
                    value = money.Value;
                    break;
                case OptionSetValue option:
                    value = !string.IsNullOrEmpty(attributeName) && record.FormattedValues.ContainsKey(attributeName)
                        ? (object)record.FormattedValues[attributeName]
                        : option.Value;
                    break;
            }

            return value is IFormattable formattable
                ? formattable.ToString(format, CultureInfo.CurrentCulture)
                : value.ToString();
        }

        /// <summary>
        /// The key the first &lt;attribute&gt; of a fetch query has in the returned records: its alias when it
        /// has one, "linkalias.name" inside an aliased link-entity, otherwise its name.
        /// </summary>
        /// <returns>The key, or null when the fetch has no attribute element or cannot be parsed.</returns>
        public static string GetFirstFetchAttributeKey(string fetchXml)
        {
            System.Xml.Linq.XElement attribute;

            try
            {
                attribute = System.Xml.Linq.XElement.Parse(fetchXml).Descendants("attribute").FirstOrDefault();
            }
            catch (XmlException)
            {
                return null;
            }

            if (attribute == null)
            {
                return null;
            }

            var alias = (string)attribute.Attribute("alias");

            if (!string.IsNullOrEmpty(alias))
            {
                return alias;
            }

            var name = (string)attribute.Attribute("name");
            var linkAlias = attribute.Parent?.Name.LocalName == "link-entity" ? (string)attribute.Parent.Attribute("alias") : null;

            return string.IsNullOrEmpty(linkAlias) ? name : $"{linkAlias}.{name}";
        }

        /// <summary>
        /// A record's value for the fetch's first attribute (see <see cref="GetFirstFetchAttributeKey"/>), unwrapped
        /// from AliasedValue. When the key is null, the record's first returned attribute is used.
        /// </summary>
        /// <returns>The value, or null when the record does not have it.</returns>
        public static object GetFirstFetchValue(Entity record, string key)
        {
            var value = key != null ? record.GetAttributeValue<object>(key) : record.Attributes.FirstOrDefault().Value;

            return value is AliasedValue aliasedValue ? aliasedValue.Value : value;
        }

        /// <summary>
        /// Count (every record), and Sum, Average, Min and Max over the records that have a value.
        /// </summary>
        /// <param name="values">One entry per record; null where the record has no numeric value.</param>
        public static RollupResult CalculateRollup(IList<decimal?> values)
        {
            var numbers = values.Where(v => v.HasValue).Select(v => v.Value).ToList();

            return new RollupResult(
                values.Count,
                numbers.Sum(),
                numbers.Count > 0 ? numbers.Average() : 0,
                numbers.Count > 0 ? numbers.Min() : 0,
                numbers.Count > 0 ? numbers.Max() : 0);
        }
    }
}
