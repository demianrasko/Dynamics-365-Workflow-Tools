using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
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

            if (read) mask |= AccessRights.ReadAccess;
            if (write) mask |= AccessRights.WriteAccess;
            if (append) mask |= AccessRights.AppendAccess;
            if (appendTo) mask |= AccessRights.AppendToAccess;
            if (delete) mask |= AccessRights.DeleteAccess;
            if (share) mask |= AccessRights.ShareAccess;
            if (assign) mask |= AccessRights.AssignAccess;

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
            }

            if (objectTypeCode == null && parameters.Length > 0)
            {
                objectTypeCode = parameters[0].Replace("etc=", string.Empty);
            }

            if (id == null && parameters.Length > 1)
            {
                id = parameters[1].Replace("id=", string.Empty);
            }

            return new RecordUrl(objectTypeCode, id);
        }

        /// <summary>
        /// Adds FetchXML paging attributes (paging-cookie, page, count) to a fetch query.
        /// A null cookie, or a page or count of 0, leaves that attribute out.
        /// </summary>
        public static string CreateXml(string xml, string cookie, int page, int count)
        {
            var stringReader = new StringReader(xml);
            var reader = new XmlTextReader(stringReader);

            // Load document
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
            var attrs = doc.DocumentElement.Attributes;

            if (cookie != null)
            {
                var pagingAttr = doc.CreateAttribute("paging-cookie");
                pagingAttr.Value = cookie;
                attrs.Append(pagingAttr);
            }

            if (page > 0)
            {
                var pageAttr = doc.CreateAttribute("page");
                pageAttr.Value = Convert.ToString(page);
                attrs.Append(pageAttr);
            }

            if (count > 0)
            {
                var countAttr = doc.CreateAttribute("count");
                countAttr.Value = Convert.ToString(count);
                attrs.Append(countAttr);
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

        /// <summary>
        /// Finds the copy of a security role that belongs to a team's or user's business unit.
        /// Roles are copied into every business unit and a principal can only hold the copy from its own
        /// business unit, so this reads the role's root role and returns the role with that root in the
        /// principal's business unit.
        /// </summary>
        /// <param name="service">Organization service.</param>
        /// <param name="tracingService">Tracing service.</param>
        /// <param name="principal">The team or systemuser whose business unit is used.</param>
        /// <param name="roleId">Any copy of the role (usually the one picked in the workflow).</param>
        /// <returns>The role id in the principal's business unit, or null if <paramref name="roleId"/> does not exist.</returns>
        public static Guid? GetRoleIdInBusinessUnit(IOrganizationService service, ITracingService tracingService, EntityReference principal, Guid roleId)
        {
            var principalRecord = service.Retrieve(principal.LogicalName, principal.Id, new ColumnSet("businessunitid"));
            var businessUnit = (EntityReference)principalRecord.Attributes["businessunitid"];

            var roleQuery = new QueryExpression
            {
                EntityName = "role",
                ColumnSet = new ColumnSet("parentrootroleid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = "roleid",
                            Operator = ConditionOperator.Equal,
                            Values = { roleId }
                        }
                    }
                }
            };

            var givenRoles = service.RetrieveMultiple(roleQuery);

            if (givenRoles.Entities.Count <= 0)
            {
                return null;
            }

            var givenRole = givenRoles.Entities[0];
            var rootRole = (EntityReference)givenRole.Attributes["parentrootroleid"];

            tracingService.Trace("Role {0} is retrieved.", givenRole.Id);

            var businessUnitRoleQuery = new QueryExpression
            {
                EntityName = "role",
                ColumnSet = new ColumnSet("roleid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = "parentrootroleid",
                            Operator = ConditionOperator.Equal,
                            Values = { rootRole.Id }
                        },
                        new ConditionExpression
                        {
                            AttributeName = "businessunitid",
                            Operator = ConditionOperator.Equal,
                            Values = { businessUnit.Id }
                        }
                    }
                }
            };

            var businessUnitRoles = service.RetrieveMultiple(businessUnitRoleQuery);

            return (Guid)businessUnitRoles.Entities[0].Attributes["roleid"];
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
            switch (value)
            {
                case null:
                    return null;
                case AliasedValue aliasedValue:
                    return AttributeValueToString(aliasedValue.Value);
                case OptionSetValue optionSetValue:
                    return optionSetValue.Value.ToString();
                case OptionSetValueCollection optionSetValues:
                    return string.Join(",", optionSetValues.Select(o => o.Value));
                case EntityReference entityReference:
                    return entityReference.Id.ToString();
                case Money money:
                    return money.Value.ToString();
                default:
                    return value.ToString();
            }
        }
        #endregion

        #region JSON
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
                using (var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(response ?? string.Empty)) { FloatParseHandling = Newtonsoft.Json.FloatParseHandling.Decimal })
                {
                    json = JObject.Load(reader);
                }
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                throw new InvalidPluginExecutionException($"Currency conversion from {fromCurrency} to {toCurrency} returned an unexpected response: {response}");
            }

            var rate = json["rates"]?[toCurrency];
            if (rate == null || rate.Type == JTokenType.Null)
            {
                var reason = (string)json["message"] ?? "no rate returned";
                throw new InvalidPluginExecutionException(
                    $"Currency conversion from {fromCurrency} to {toCurrency} is not available ({reason}). Supported currencies are the ECB reference currencies, e.g. USD, EUR, GBP, JPY, CAD, AUD, CHF.");
            }

            return rate.Value<decimal>();
        }
        #endregion
    }

    /// <summary>
    /// The parts of a Dynamics record URL returned by <see cref="Utility.ParseRecordUrl"/>.
    /// </summary>
    public sealed class RecordUrl
    {
        public RecordUrl(string objectTypeCode, string id)
        {
            ObjectTypeCode = objectTypeCode;
            Id = id;
        }

        /// <summary>The entity type code from the "etc" parameter.</summary>
        public string ObjectTypeCode { get; }

        /// <summary>The record id from the "id" parameter.</summary>
        public string Id { get; }
    }
}
