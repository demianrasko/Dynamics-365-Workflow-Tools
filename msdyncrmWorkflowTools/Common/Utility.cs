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
        /// <returns>The object type code and id, as the strings they appear in the URL.</returns>
        /// <exception cref="InvalidPluginExecutionException">The URL is empty or has no query string.</exception>
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

            return new RecordUrl(objectTypeCode, id, entityName);
        }

        public static string GetRecordId(string recordUrl)
        {
            return string.IsNullOrEmpty(recordUrl) ? string.Empty : ParseRecordUrl(recordUrl).Id;
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
                case "from":
                    sReturn = "1";
                    break;
                case "to":
                    sReturn = "2";
                    break;
                case "cc":
                    sReturn = "3";
                    break;
                case "bcc":
                    sReturn = "4";
                    break;
                case "organizer":
                    sReturn = "7";
                    break;
                case "requiredattendees":
                    sReturn = "5";
                    break;
                case "optionalattendees":
                    sReturn = "6";
                    break;
                case "customer":
                    sReturn = "11";
                    break;
                case "resources":
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
        #endregion

        #region Rollups
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
        public static string JsonParser(string Json, string JsonPath)
        {
            if (JsonPath == null)
            {
                JsonPath = string.Empty;
            }
            var o = JObject.Parse(Json);
            var name = string.Empty;
            if (o.SelectToken(JsonPath) != null)
            {
                name = o.SelectToken(JsonPath).ToString();
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
        public static string SerializeEntity(string entityName, string primaryIdAttribute, string id, Entity record, IEnumerable<string> attributes)
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
            ref int DayOfWeek, ref int DayOfYear, ref int Day, ref int Month, ref int Year, ref int WeekOfYear)
        {
            difference = date1 - date2;
            DayOfWeek = (int)date1.DayOfWeek;
            DayOfYear = date1.DayOfYear;
            Day = date1.Day;
            Month = date1.Month;
            Year = date1.Year;
            var dfi = DateTimeFormatInfo.CurrentInfo;
            var cal = dfi.Calendar;
            WeekOfYear = cal.GetWeekOfYear(date1, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);

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
            if (string.IsNullOrEmpty(replaceOldValue))
            {
                replacedText = inputText;
            }
            else if (caseSensitive)
            {
                replacedText = inputText.Replace(replaceOldValue, replaceNewValue ?? string.Empty);
            }
            else
            {
                replacedText = CompareAndReplace(inputText, replaceOldValue, replaceNewValue ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
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
        private static readonly HttpClient httpClient;

        /// <summary>
        /// Class Constructor: Inits Singletion objects
        /// </summary>
        static Utility()
        {
            //Setup a commong HttpClient as a best practice to avoid leaving open connections
            //more details https://docs.microsoft.com/en-us/azure/architecture/antipatterns/improper-instantiation/
            httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(0, 0, 30); //30 second timeout as recommend by Microsoft Support to prevent TimeOut on Sandbox
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
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
                response = Task.Run(() => httpClient.SendAsync(message)).GetAwaiter().GetResult();
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
        public RecordUrl(string objectTypeCode, string id, string entityName)
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
        public string Id { get; }
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
}
