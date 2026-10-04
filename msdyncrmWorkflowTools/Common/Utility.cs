using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace msdyncrmWorkflowTools
{
    /// <summary>
    /// General-purpose helpers shared by the workflow activities and Common. Keep methods here static and free of workflow state.
    /// </summary>
    public static class Utility
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
