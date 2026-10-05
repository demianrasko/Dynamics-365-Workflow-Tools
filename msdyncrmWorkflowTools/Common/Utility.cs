using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// General-purpose helpers shared by the workflow activities and Common. Keep methods here static and free of workflow state.
    /// </summary>
    public static class Utility
    {
        #region Error Handling
        /// <summary>
        /// Formats an exception as text for the tracing service or an error message: type, message,
        /// Dataverse fault details (error code, timestamp, activity id, trace text, nested inner faults),
        /// stack trace, and the same for every inner exception.
        /// </summary>
        /// <remarks>
        /// The result can contain { and } (stack traces, JSON, FetchXML), so trace it as
        /// <c>tracingService.Trace("{0}", Utility.HandleExceptions(ex))</c> rather than passing it as the format string.
        /// </remarks>
        /// <param name="ex">The exception to format. Null returns an empty string.</param>
        /// <returns>The formatted exception details.</returns>
        public static string HandleExceptions(Exception ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current != ex)
                {
                    sb.AppendLine("--- Inner exception ---");
                }

                AppendException(current, sb);
            }

            return sb.ToString();
        }

        private static void AppendException(Exception ex, StringBuilder sb)
        {
            sb.AppendLine($"Type:\t{ex.GetType().FullName}");
            sb.AppendLine($"Message:\t{ex.Message}");

            switch (ex)
            {
                case FaultException<OrganizationServiceFault> organizationFault:
                    for (var fault = organizationFault.Detail; fault != null; fault = fault.InnerFault)
                    {
                        if (fault != organizationFault.Detail)
                        {
                            sb.AppendLine("--- Inner fault ---");
                        }

                        AppendFault(fault, sb);

                        if (!string.IsNullOrEmpty(fault.TraceText))
                        {
                            sb.AppendLine($"Trace:\t{fault.TraceText}");
                        }
                    }

                    break;

                case FaultException<DiscoveryServiceFault> discoveryFault:
                    for (var fault = discoveryFault.Detail; fault != null; fault = fault.InnerFault)
                    {
                        if (fault != discoveryFault.Detail)
                        {
                            sb.AppendLine("--- Inner fault ---");
                        }

                        AppendFault(fault, sb);
                    }

                    break;

                case WebException webException:
                    sb.AppendLine($"Status:\t{webException.Status}");
                    break;
            }

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.AppendLine($"Stack Trace:\t{ex.StackTrace}");
            }
        }

        private static void AppendFault(BaseServiceFault fault, StringBuilder sb)
        {
            sb.AppendLine($"Code:\t{fault.ErrorCode}");
            sb.AppendLine($"Fault Message:\t{fault.Message}");
            sb.AppendLine($"Timestamp:\t{fault.Timestamp}");
            sb.AppendLine($"Activity Id:\t{fault.ActivityId}");
        }
        #endregion

        /// <summary>
        /// Builds the access mask for a GrantAccessRequest from the individual share flags.
        /// </summary>
        /// <param name="read">Grant read access.</param>
        /// <param name="write">Grant write access.</param>
        /// <param name="append">Grant append access.</param>
        /// <param name="appendTo">Grant append-to access.</param>
        /// <param name="delete">Grant delete access.</param>
        /// <param name="share">Grant share access.</param>
        /// <param name="assign">Grant assign access.</param>
        /// <returns>The combined <see cref="AccessRights"/>, or <see cref="AccessRights.None"/> if no flag is set.</returns>
        public static AccessRights GetMask(bool read, bool write, bool append, bool appendTo, bool delete, bool share, bool assign)
        {
            var mask = AccessRights.None;

            if (read)
            {
                mask |= AccessRights.ReadAccess;
            }
            if (write)
            {
                mask |= AccessRights.WriteAccess;
            }
            if (append)
            {
                mask |= AccessRights.AppendAccess;
            }
            if (appendTo)
            {
                mask |= AccessRights.AppendToAccess;
            }
            if (delete)
            {
                mask |= AccessRights.DeleteAccess;
            }
            if (share)
            {
                mask |= AccessRights.ShareAccess;
            }
            if (assign)
            {
                mask |= AccessRights.AssignAccess;
            }

            return mask;
        }

        /// <summary>
        /// Splits a Dynamics record URL (the "Record URL (Dynamic)" value a workflow passes in) into its
        /// object type code ("etc") and record id ("id") query parameters.
        /// </summary>
        /// <remarks>
        /// Parameters are found by name, so the order of the query string does not matter. If a parameter
        /// is not found by name, the value falls back to the old positional parsing (etc first, id second),
        /// so URLs that worked before return exactly the same values.
        /// </remarks>
        /// <param name="recordUrl">The record URL, e.g. https://org.crm.dynamics.com/main.aspx?etc=1&amp;id=...&amp;pagetype=entityrecord</param>
        /// <returns>The object type code, the record id and, when the URL has an "etn" parameter, the entity name.</returns>
        /// <exception cref="InvalidPluginExecutionException">The URL is empty, has no query string or has no valid record id.</exception>
        public static RecordUrl ParseRecordUrl(string recordUrl)
        {
            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("The record URL is empty.");
            }

            var queryStart = recordUrl.IndexOf('?');
            if (queryStart < 0)
            {
                throw new InvalidPluginExecutionException($"The record URL '{recordUrl}' has no query string.");
            }

            var parameters = recordUrl.Substring(queryStart + 1).Split('&');
            string objectTypeCode = null;
            string id = null;
            string entityName = null;

            foreach (var parameter in parameters)
            {
                var separator = parameter.IndexOf('=');
                if (separator < 0)
                {
                    continue;
                }

                var name = parameter.Substring(0, separator);
                var value = parameter.Substring(separator + 1);

                if (objectTypeCode == null && string.Equals(name, "etc", StringComparison.OrdinalIgnoreCase))
                {
                    objectTypeCode = value;
                }
                else if (id == null && string.Equals(name, "id", StringComparison.OrdinalIgnoreCase))
                {
                    id = value;
                }
                else if (entityName == null && string.Equals(name, "etn", StringComparison.OrdinalIgnoreCase))
                {
                    entityName = value;
                }
            }

            if (objectTypeCode == null && parameters.Length > 0)
            {
                objectTypeCode = parameters[0].Replace("etc=", string.Empty);
            }

            if (id == null && parameters.Length > 1)
            {
                id = parameters[1].Replace("id=", string.Empty);
            }

            // ids may be wrapped in braces, literally or URL-encoded (%7B...%7D)
            if (!Guid.TryParse(Uri.UnescapeDataString(id ?? string.Empty).Trim(), out var recordId))
            {
                throw new InvalidPluginExecutionException($"The record URL '{recordUrl}' does not contain a valid record id.");
            }

            return new RecordUrl(objectTypeCode, recordId, entityName);
        }

        public static string GetRecordId(string recordUrl)
        {
            return string.IsNullOrEmpty(recordUrl) ? string.Empty : ParseRecordUrl(recordUrl).Id.ToString();
        }

        /// <summary>
        /// A record URL for a record in the same environment as <paramref name="referenceRecordUrl"/>: its address
        /// (everything before "?") with etc, id, etn and pagetype parameters.
        /// </summary>
        public static string BuildRecordUrl(string referenceRecordUrl, int objectTypeCode, string entityName, Guid id)
        {
            var queryStart = referenceRecordUrl.IndexOf('?');
            var address = queryStart < 0 ? referenceRecordUrl : referenceRecordUrl.Substring(0, queryStart);

            return $"{address}?etc={objectTypeCode}&id={id}&etn={Uri.EscapeDataString(entityName)}&pagetype=entityrecord";
        }

        /// <summary>
        /// Adds FetchXML paging attributes (paging-cookie, page, count) to a fetch query.
        /// A null cookie, or a page or count of 0, leaves that attribute out.
        /// </summary>
        public static string CreateXml(string xml, string cookie, int page, int count)
        {
            var stringReader = new StringReader(xml);
            var reader = new XmlTextReader(stringReader);

            var doc = new XmlDocument();

            doc.Load(reader);

            return CreateXml(doc, cookie, page, count);
        }

        /// <summary>
        /// Adds FetchXML paging attributes (paging-cookie, page, count) to a loaded fetch document.
        /// </summary>
        public static string CreateXml(XmlDocument doc, string cookie, int page, int count)
        {
            if (doc.DocumentElement == null)
            {
                return string.Empty;
            }
            var attributes = doc.DocumentElement.Attributes;

            if (cookie != null)
            {
                var pagingCookie = doc.CreateAttribute("paging-cookie");

                pagingCookie.Value = cookie;
                attributes.Append(pagingCookie);
            }

            if (page > 0)
            {
                var pageAttribute = doc.CreateAttribute("page");

                pageAttribute.Value = Convert.ToString(page);
                attributes.Append(pageAttribute);
            }

            if (count > 0)
            {
                var countAttribute = doc.CreateAttribute("count");

                countAttribute.Value = Convert.ToString(count);
                attributes.Append(countAttribute);
            }

            var sb = new StringBuilder(1024);
            var stringWriter = new StringWriter(sb);

            var writer = new XmlTextWriter(stringWriter);
            doc.WriteTo(writer);
            writer.Close();

            return sb.ToString();
        }

        /// <summary>
        /// Maps an activity party attribute name (from, to, cc, ...) to its participationtypemask value,
        /// or returns an empty string for an unknown name.
        /// </summary>
        public static string GetParticipation(string attributeName)
        {
            var sReturn = string.Empty;

            switch (attributeName)
            {
                case AttributeNames.From:
                    sReturn = "1";
                    break;
                case AttributeNames.To:
                    sReturn = "2";
                    break;
                case AttributeNames.Cc:
                    sReturn = "3";
                    break;
                case AttributeNames.Bcc:
                    sReturn = "4";
                    break;
                case AttributeNames.Organizer:
                    sReturn = "7";
                    break;
                case AttributeNames.RequiredAttendees:
                    sReturn = "5";
                    break;
                case AttributeNames.OptionalAttendees:
                    sReturn = "6";
                    break;
                case AttributeNames.Customer:
                    sReturn = "11";
                    break;
                case AttributeNames.Resources:
                    sReturn = "10";
                    break;
            }

            return sReturn;
        }

        #region Attribute values

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
        #endregion

        #region Rollups
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

            return string.Format($"{{0:{format}}}", value);
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
        /// A numeric attribute value (number, Money or an AliasedValue wrapping one) as a decimal; null for
        /// anything else, including a missing value.
        /// </summary>
        public static decimal? ToDecimal(object value)
        {
            switch (value)
            {
                case AliasedValue aliasedValue:
                    return ToDecimal(aliasedValue.Value);
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
        #endregion

        #region JSON
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
        #endregion

        #region Text and dates
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
        #endregion

        #region Hashing
        /// <summary>
        /// MD5 hash of the text's ASCII bytes, as 32 lowercase hex digits.
        /// </summary>
        public static string Md5Hash(string text)
        {
            using (var md5 = MD5.Create())
            {
                return ToHex(md5.ComputeHash(Encoding.ASCII.GetBytes(text)));
            }
        }

        /// <summary>
        /// SHA-512 hash of the text's ASCII bytes, as 128 lowercase hex digits.
        /// </summary>
        public static string Sha512Hash(string text)
        {
            using (var sha512 = SHA512.Create())
            {
                return ToHex(sha512.ComputeHash(Encoding.ASCII.GetBytes(text)));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);

            foreach (var b in bytes)
            {
                hex.Append(b.ToString("x2"));
            }

            return hex.ToString();
        }
        #endregion

        #region Currency
        /// <summary>
        /// Reads the converted amount from a Frankfurter response such as
        /// <c>{"amount":10.0,"base":"USD","date":"2026-10-02","rates":{"EUR":8.9087}}</c>.
        /// </summary>
        /// <param name="response">The response body.</param>
        /// <param name="fromCurrency">Source currency code (used in the error message).</param>
        /// <param name="toCurrency">Target currency code to read from "rates".</param>
        /// <returns>The converted amount.</returns>
        /// <exception cref="InvalidPluginExecutionException">The response has no rate for <paramref name="toCurrency"/>,
        /// e.g. an unsupported currency ({"message":"not found"}).</exception>
        public static decimal ParseCurrencyConversion(string response, string fromCurrency, string toCurrency)
        {
            JObject json;
            try
            {
                using (var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(response ?? string.Empty)))
                {
                    reader.FloatParseHandling = Newtonsoft.Json.FloatParseHandling.Decimal;
                    json = JObject.Load(reader);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                throw new InvalidPluginExecutionException($"Currency conversion from {fromCurrency} to {toCurrency} returned an unexpected response: {response}");
            }

            var rate = json["rates"]?[toCurrency];
            if (rate != null && rate.Type != JTokenType.Null)
            {
                return rate.Value<decimal>();
            }

            var reason = (string)json["message"] ?? "no rate returned";

            throw new InvalidPluginExecutionException(
                $"Currency conversion from {fromCurrency} to {toCurrency} is not available ({reason}). Supported currencies are the ECB reference currencies, e.g. USD, EUR, GBP, JPY, CAD, AUD, CHF.");
        }
        #endregion

        #region Translator
        /// <summary>
        /// Builds the Azure AI Translator v3 request body: <c>[{"Text": "..."}]</c>.
        /// </summary>
        public static string BuildTranslatorRequest(string text)
        {
            return new JArray(new JObject { ["Text"] = text }).ToString(Newtonsoft.Json.Formatting.None);
        }

        /// <summary>
        /// Reads the translated text from an Azure AI Translator v3 response, e.g.
        /// <c>[{"detectedLanguage":{"language":"en","score":1.0},"translations":[{"text":"Hola","to":"es"}]}]</c>.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The service returned an error
        /// (<c>{"error":{"code":401001,"message":"..."}}</c>) or an unexpected response.</exception>
        public static string ParseTranslatorResponse(string response)
        {
            JToken json;
            try
            {
                json = JToken.Parse(response ?? string.Empty);
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                throw new InvalidPluginExecutionException($"Translator returned an unexpected response: {response}");
            }

            if (json is JObject error && error["error"] != null)
            {
                throw new InvalidPluginExecutionException(
                    $"Translator error {(string)error["error"]["code"]}: {(string)error["error"]["message"]}");
            }

            var text = json.SelectToken("[0].translations[0].text");
            if (text == null)
            {
                throw new InvalidPluginExecutionException($"Translator returned an unexpected response: {response}");
            }

            return (string)text;
        }
        #endregion

        #region External web services (not Dataverse)
        // Shared HttpClient for the external web services
        private static readonly HttpClient HttpClient;

        /// <summary>
        /// Class Constructor: Inits Singletion objects
        /// </summary>
        static Utility()
        {
            //Setup a commong HttpClient as a best practice to avoid leaving open connections
            //more details https://docs.microsoft.com/en-us/azure/architecture/antipatterns/improper-instantiation/
            HttpClient = new HttpClient();
            HttpClient.Timeout = new TimeSpan(0, 0, 30); //30 second timeout as recommend by Microsoft Support to prevent TimeOut on Sandbox
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
        }

        private static void Trace(ITracingService tracingService, string format, params object[] args)
        {
            tracingService?.Trace(format, args);
        }

        /// <summary>
        /// Translates text with Azure AI Translator (Text Translation v3.0).
        /// </summary>
        /// <param name="textToTranslate">Text to translate; the source language is detected automatically.</param>
        /// <param name="language">Target language code, e.g. "en", "es", "pt", "fr-ca".</param>
        /// <param name="key">Translator resource key (Ocp-Apim-Subscription-Key).</param>
        /// <param name="region">Azure region of the Translator resource, e.g. "westeurope". Required for regional and
        /// multi-service resources; leave empty for a global Translator resource.</param>
        /// <param name="tracingService">Optional tracing service for the response.</param>
        /// <returns>The translated text, or an empty string when there is nothing to translate.</returns>
        public static string TranslateText(string textToTranslate, string language, string key, string region = null, ITracingService tracingService = null)
        {
            if (string.IsNullOrEmpty(textToTranslate))
            {
                return string.Empty;
            }

            var url = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=" +
                      Uri.EscapeDataString((language ?? string.Empty).Trim());

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(BuildTranslatorRequest(textToTranslate), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);

            if (!string.IsNullOrWhiteSpace(region))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Region", region.Trim());
            }

            var response = ExecuteAsyncRequest(request, tracingService);
            Trace(tracingService, "Translator response: {0}", response);

            return ParseTranslatorResponse(response);
        }

        /// <summary>
        /// Converts an amount between currencies using the European Central Bank reference rates published by
        /// Frankfurter (https://frankfurter.dev, free, no API key). Rates are updated once per working day.
        /// </summary>
        /// <remarks>Only the ~30 currencies the ECB publishes are supported (USD, EUR, GBP, JPY, CAD, AUD, ...).</remarks>
        /// <param name="amount">Amount in <paramref name="fromCurrency"/>.</param>
        /// <param name="fromCurrency">ISO 4217 code, e.g. USD.</param>
        /// <param name="toCurrency">ISO 4217 code, e.g. EUR.</param>
        /// <param name="tracingService">Optional tracing service for the request and response.</param>
        /// <returns>The converted amount in <paramref name="toCurrency"/>.</returns>
        public static decimal CurrencyConvert(decimal amount, string fromCurrency, string toCurrency, ITracingService tracingService = null)
        {
            var from = (fromCurrency ?? string.Empty).Trim().ToUpperInvariant();
            var to = (toCurrency ?? string.Empty).Trim().ToUpperInvariant();

            if (from == to)
            {
                return amount;
            }

            var url = string.Format(CultureInfo.InvariantCulture,
                "https://api.frankfurter.dev/v1/latest?amount={0}&base={1}&symbols={2}",
                amount, Uri.EscapeDataString(from), Uri.EscapeDataString(to));

            Trace(tracingService, "Currency conversion request: {0}", url);
            var response = ExecuteAsyncRequest(new HttpRequestMessage(HttpMethod.Get, url), tracingService);
            Trace(tracingService, "Currency conversion response: {0}", response);

            return ParseCurrencyConversion(response, from, to);
        }

        /// <summary>
        /// Finds the latitude and longitude of an address with Azure Maps when an Azure Maps key is given,
        /// otherwise with Bing Maps (Bing Maps for Enterprise keys stop working on June 30, 2028).
        /// </summary>
        /// <param name="address">The address, e.g. "1 Microsoft Way, Redmond, WA".</param>
        /// <param name="bingMapsKey">Bing Maps key; ignored when <paramref name="azureMapsKey"/> is set.</param>
        /// <param name="azureMapsKey">Azure Maps subscription key; empty to use Bing Maps.</param>
        /// <param name="tracingService">Optional tracing service for the request.</param>
        /// <returns>The location, or null when the address is empty or was not found.</returns>
        public static GeoLocation GeocodeAddress(string address, string bingMapsKey, string azureMapsKey = null, ITracingService tracingService = null)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            var useAzureMaps = !string.IsNullOrWhiteSpace(azureMapsKey);
            Trace(tracingService, "Geocoding with {0}", useAzureMaps ? "Azure Maps" : "Bing Maps");

            var url = useAzureMaps ? BuildAzureMapsGeocodeUrl(address, azureMapsKey) : BuildBingGeocodeUrl(address, bingMapsKey);
            var response = ExecuteAsyncRequest(new HttpRequestMessage(HttpMethod.Get, url), tracingService);

            return useAzureMaps ? ParseAzureMapsGeocodeResponse(response) : ParseBingGeocodeResponse(response);
        }

        /// <summary>
        /// Bing Maps Locations API request for an address (one result).
        /// </summary>
        public static string BuildBingGeocodeUrl(string address, string key)
        {
            var query = Uri.EscapeDataString((address ?? string.Empty).Trim());

            return $"https://dev.virtualearth.net/REST/v1/Locations?maxResults=1&query={query}&key={Uri.EscapeDataString((key ?? string.Empty).Trim())}";
        }

        /// <summary>
        /// Azure Maps Geocoding API (2023-06-01) request for an address (one result).
        /// </summary>
        public static string BuildAzureMapsGeocodeUrl(string address, string key)
        {
            var query = Uri.EscapeDataString((address ?? string.Empty).Trim());

            return $"https://atlas.microsoft.com/geocode?api-version=2023-06-01&top=1&query={query}&subscription-key={Uri.EscapeDataString((key ?? string.Empty).Trim())}";
        }

        /// <summary>
        /// Reads the first location from a Bing Maps Locations response.
        /// </summary>
        /// <returns>The location, or null when nothing was found.</returns>
        /// <exception cref="InvalidPluginExecutionException">Bing Maps returned an error.</exception>
        public static GeoLocation ParseBingGeocodeResponse(string json)
        {
            var root = JObject.Parse(json);
            var statusCode = (int?)root["statusCode"];

            if (statusCode.HasValue && statusCode.Value != 200)
            {
                var details = root["errorDetails"]?.Values<string>().ToArray() ?? new string[0];
                var message = details.Length > 0 ? string.Join(" ", details) : (string)root["statusDescription"];

                throw new InvalidPluginExecutionException($"Bing Maps error {statusCode}: {message}");
            }

            var resource = root.SelectToken("resourceSets[0].resources[0]");
            var coordinates = resource?.SelectToken("geocodePoints[0].coordinates") ?? resource?.SelectToken("point.coordinates");

            // Bing returns [latitude, longitude]
            return coordinates == null ? null : new GeoLocation((decimal)coordinates[0], (decimal)coordinates[1]);
        }

        /// <summary>
        /// Reads the first location from an Azure Maps Geocoding response (GeoJSON).
        /// </summary>
        /// <returns>The location, or null when nothing was found.</returns>
        /// <exception cref="InvalidPluginExecutionException">Azure Maps returned an error.</exception>
        public static GeoLocation ParseAzureMapsGeocodeResponse(string json)
        {
            var root = JObject.Parse(json);
            var error = root["error"];

            if (error != null)
            {
                throw new InvalidPluginExecutionException($"Azure Maps error {(string)error["code"]}: {(string)error["message"]}");
            }

            var coordinates = root.SelectToken("features[0].geometry.coordinates");

            // GeoJSON is [longitude, latitude]
            return coordinates == null ? null : new GeoLocation((decimal)coordinates[1], (decimal)coordinates[0]);
        }

        /// <summary>
        /// Sends an HTTP request synchronously (shared HttpClient, 30-second timeout) and returns the response body.
        /// </summary>
        /// <remarks>
        /// An error status with a JSON body is returned to the caller, which turns the API's own error message into a
        /// readable exception (Frankfurter, Translator). Any other error status throws with the status and the start of the body.
        /// </remarks>
        /// <param name="message">The request to send.</param>
        /// <param name="tracingService">Optional tracing service for the response status.</param>
        /// <returns>The response body.</returns>
        private static string ExecuteAsyncRequest(HttpRequestMessage message, ITracingService tracingService)
        {
            HttpResponseMessage response;
            string body;

            try
            {
                // Task.Run keeps the async calls off any synchronization context; GetResult blocks without spinning
                response = Task.Run(() => HttpClient.SendAsync(message)).GetAwaiter().GetResult();
                body = Task.Run(() => response.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException($"Timeout waiting for HttpResponse {message.Method}:{message.RequestUri}");
            }

            Trace(tracingService, "HTTP {0} {1} from {2}", (int)response.StatusCode, response.ReasonPhrase, message.RequestUri.Host);

            var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (!response.IsSuccessStatusCode && mediaType.IndexOf("json", StringComparison.OrdinalIgnoreCase) < 0)
            {
                var detail = body.Length > 500 ? $"{body.Substring(0, 500)}..." : body;
                throw new InvalidPluginExecutionException(
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} from {message.RequestUri.Host}: {detail}");
            }

            return body;
        }
        #endregion
    }

    /// <summary>
    /// The parts of a Dynamics record URL returned by <see cref="Utility.ParseRecordUrl"/>.
    /// </summary>
    public sealed class RecordUrl
    {
        public RecordUrl(string objectTypeCode, Guid id, string entityName)
        {
            ObjectTypeCode = objectTypeCode;
            Id = id;
            EntityName = entityName;
        }

        /// <summary>The entity type code from the "etc" parameter.</summary>
        public string ObjectTypeCode { get; }

        /// <summary>
        /// The entity logical name: the "etn" parameter when the URL has one (Unified Interface URLs), otherwise
        /// null from <see cref="Utility.ParseRecordUrl"/> and looked up from the type code by Common.ParseRecordUrl.
        /// </summary>
        public string EntityName { get; }

        /// <summary>The record id from the "id" parameter.</summary>
        public Guid Id { get; }

        /// <summary>The record as an EntityReference (needs <see cref="EntityName"/>).</summary>
        public EntityReference ToEntityReference()
        {
            return new EntityReference(EntityName, Id);
        }
    }

    /// <summary>
    /// The results of <see cref="Utility.CalculateRollup"/>.
    /// </summary>
    public sealed class RollupResult
    {
        public RollupResult(decimal count, decimal sum, decimal average, decimal min, decimal max)
        {
            Count = count;
            Sum = sum;
            Average = average;
            Min = min;
            Max = max;
        }

        public decimal Count { get; }

        public decimal Sum { get; }

        public decimal Average { get; }

        public decimal Min { get; }

        public decimal Max { get; }
    }

    /// <summary>
    /// A latitude and longitude returned by <see cref="Utility.GeocodeAddress"/>.
    /// </summary>
    public sealed class GeoLocation
    {
        public GeoLocation(decimal latitude, decimal longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public decimal Latitude { get; }

        public decimal Longitude { get; }
    }
}
