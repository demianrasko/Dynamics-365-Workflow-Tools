using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public static partial class Utility
    {
        public static string JsonParser(string json, string jsonPath)
        {
            if (jsonPath == null)
            {
                jsonPath = string.Empty;
            }
            var o = JObject.Parse(json);
            var name = string.Empty;

            if (o.SelectToken(jsonPath) != null)
            {
                name = o.SelectToken(jsonPath)?.ToString();
            }
            return name;
        }

        /// <summary>
        /// Serializes a record as <c>{"entityname": {"primaryid": "id", "attribute": value, ...}}</c>,
        /// the format the Entity JSON Serializer activity has always produced.
        /// </summary>
        /// <remarks>
        /// Text is escaped properly, numbers use invariant formatting, dates are ISO 8601 strings, lookups are
        /// <c>{"typename": ..., "id": ..., "name": ...}</c>, option sets are their number, money is its amount
        /// and multi-select option sets are an array of numbers. Attributes missing from the record are skipped.
        /// </remarks>
        /// <param name="entityName">Logical name used as the root property.</param>
        /// <param name="primaryIdAttribute">Name of the primary key attribute, written first.</param>
        /// <param name="id">Record id written for the primary key.</param>
        /// <param name="record">The retrieved record.</param>
        /// <param name="attributes">Attributes to include, in order.</param>
        public static string SerializeEntity(string entityName, string primaryIdAttribute, Guid id, Entity record, IEnumerable<string> attributes)
        {
            var body = new JObject
            {
                [primaryIdAttribute] = id
            };

            foreach (var attribute in attributes)
            {
                if (record.Attributes.Contains(attribute))
                {
                    body[attribute] = ToJsonValue(record.Attributes[attribute]);
                }
            }

            var root = new JObject
            {
                [entityName] = body
            };

            return root.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static JToken ToJsonValue(object value)
        {
            switch (value)
            {
                case null:
                    return JValue.CreateNull();
                case AliasedValue aliasedValue:
                    return ToJsonValue(aliasedValue.Value);
                case OptionSetValue optionSetValue:
                    return optionSetValue.Value;
                case OptionSetValueCollection optionSetValues:
                    return new JArray(optionSetValues.Select(o => o.Value));
                case Money money:
                    return money.Value;
                case EntityReference entityReference:
                    return new JObject
                    {
                        ["typename"] = entityReference.LogicalName?.ToLower(),
                        ["id"] = entityReference.Id.ToString(),
                        ["name"] = entityReference.Name
                    };
                case DateTime dateTime:
                    return dateTime.ToString("o", CultureInfo.InvariantCulture);
                case Guid guid:
                    return guid.ToString();
                case string _:
                case bool _:
                case int _:
                case long _:
                case decimal _:
                case double _:
                    return JToken.FromObject(value);
                default:
                    return value.ToString();
            }
        }
    }
}
