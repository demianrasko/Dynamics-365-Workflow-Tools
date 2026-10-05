using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// General-purpose helpers shared by the workflow activities and Common. Keep methods here static and free of workflow state.
    /// </summary>
    public static partial class Utility
    {
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
    }
}
