using Microsoft.Xrm.Sdk;
using System;

namespace msdyncrmWorkflowTools
{
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
}
