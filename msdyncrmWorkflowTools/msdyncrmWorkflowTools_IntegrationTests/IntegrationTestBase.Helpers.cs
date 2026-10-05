using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System.Linq;

namespace msdyncrmWorkflowTools_IntegrationTests
{
    /// <summary>
    /// Small helpers the integration tests share.
    /// </summary>
    public abstract partial class IntegrationTestBase
    {
        private int contactCount;

        /// <summary>
        /// Creates a contact (deleted after the test), optionally under an account. Last names end with a counter
        /// (" 0", " 1", ...), so a test can filter on them.
        /// </summary>
        protected EntityReference CreateContact(EntityReference account)
        {
            return Create(new Entity(EntityNames.Contact)
            {
                ["lastname"] = $"{UniqueName("contact")} {contactCount++}",
                ["parentcustomerid"] = account
            });
        }

        /// <summary>A record's column values.</summary>
        protected Entity Read(EntityReference record, params string[] columns)
        {
            return Service.Retrieve(record.LogicalName, record.Id, new ColumnSet(columns));
        }

        protected int StateOf(EntityReference record)
        {
            return Read(record, AttributeNames.StateCode).GetAttributeValue<OptionSetValue>(AttributeNames.StateCode).Value;
        }

        protected static OptionSetValueCollection Options(params int[] values)
        {
            return new OptionSetValueCollection(values.Select(v => new OptionSetValue(v)).ToList());
        }

        protected static int[] Values(OptionSetValueCollection values)
        {
            return values.Select(v => v.Value).ToArray();
        }
    }
}
