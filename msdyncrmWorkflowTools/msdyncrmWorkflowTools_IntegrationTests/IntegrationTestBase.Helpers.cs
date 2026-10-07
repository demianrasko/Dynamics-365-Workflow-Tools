using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using msdyncrmWorkflowTools;
using System;
using System.Collections.Generic;
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

        /// <summary>Creates an owner team in the application user's business unit (deleted after the test).</summary>
        protected EntityReference CreateTeam()
        {
            return Create(new Entity(EntityNames.Team)
            {
                [AttributeNames.Name] = UniqueName("team"),
                [AttributeNames.BusinessUnitId] = new EntityReference(EntityNames.BusinessUnit, BusinessUnitId),
                [AttributeNames.TeamType] = new OptionSetValue(0)
            });
        }

        /// <summary>
        /// A security role of the application user's business unit that the application user doesn't have
        /// (Basic User when there is one).
        /// </summary>
        protected Guid RoleTheUserDoesNotHave()
        {
            var userRoles = new HashSet<Guid>(Service.RetrieveMultiple(new QueryExpression(EntityNames.SystemUserRoles)
            {
                ColumnSet = new ColumnSet(AttributeNames.RoleId),
                Criteria = { Conditions = { new ConditionExpression(AttributeNames.SystemUserId, ConditionOperator.Equal, UserId) } }
            }).Entities.Select(e => e.GetAttributeValue<Guid>(AttributeNames.RoleId)));

            var roles = Service.RetrieveMultiple(new QueryExpression(EntityNames.Role)
            {
                ColumnSet = new ColumnSet(AttributeNames.Name),
                Criteria = { Conditions = { new ConditionExpression(AttributeNames.BusinessUnitId, ConditionOperator.Equal, BusinessUnitId) } }
            }).Entities.Where(r => !userRoles.Contains(r.Id)).ToList();

            return (roles.FirstOrDefault(r => r.GetAttributeValue<string>(AttributeNames.Name) == "Basic User") ?? roles.First()).Id;
        }

        /// <summary>A security role the application user has.</summary>
        protected Guid RoleTheUserHas()
        {
            return Service.RetrieveMultiple(new QueryExpression(EntityNames.SystemUserRoles)
            {
                ColumnSet = new ColumnSet(AttributeNames.RoleId),
                Criteria = { Conditions = { new ConditionExpression(AttributeNames.SystemUserId, ConditionOperator.Equal, UserId) } }
            }).Entities.First().GetAttributeValue<Guid>(AttributeNames.RoleId);
        }
    }
}
