using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        /// <summary>
        /// Picks the newest unassigned items of a queue for a worker.
        /// </summary>
        /// <param name="queueId">The queue.</param>
        /// <param name="workerId">The user the items are assigned to.</param>
        /// <param name="removeItems">Remove the items from the queue.</param>
        /// <param name="quantity">How many items to pick; less than 1 picks one.</param>
        /// <returns>The number of items picked.</returns>
        public int PickFromQueue(Guid queueId, Guid workerId, bool removeItems, int quantity)
        {
            var queueItems = Service.RetrieveMultiple(QueueItemsQuery(queueId, onlyUnassigned: true, top: Math.Max(quantity, 1))).Entities;

            foreach (var queueItem in queueItems)
            {
                Service.Execute(new PickFromQueueRequest
                {
                    QueueItemId = queueItem.Id,
                    WorkerId = workerId,
                    RemoveQueueItem = removeItems
                });
            }

            Trace($"Picked {queueItems.Count} item(s) from queue {queueId}");

            return queueItems.Count;
        }

        /// <summary>
        /// The value of an organization setting (a column of the organization table), or null when it is empty.
        /// </summary>
        public object GetOrganizationSetting(string attributeName)
        {
            var organization = Service.RetrieveMultiple(OrganizationSettingQuery(attributeName)).Entities.FirstOrDefault();

            return organization?.GetAttributeValue<object>(attributeName);
        }

        /// <summary>
        /// Sets an organization setting; the value is stored as a whole number, true/false or text
        /// (see <see cref="Utility.ConvertSettingValue"/>).
        /// </summary>
        /// <returns>False when the organization record could not be read.</returns>
        public bool SetOrganizationSetting(string attributeName, string value)
        {
            var organization = Service.RetrieveMultiple(OrganizationSettingQuery(attributeName)).Entities.FirstOrDefault();

            if (organization == null)
            {
                return false;
            }

            Trace($"Organization setting {attributeName} = {value}");

            Service.Update(new Entity(organization.LogicalName, organization.Id)
            {
                [attributeName] = Utility.ConvertSettingValue(value)
            });

            return true;
        }

        /// <summary>
        /// Updates a user's personal settings (see <see cref="Utility.BuildUserSettings"/> for which are written).
        /// </summary>
        public void SetUserSettings(Guid userId, int pagingLimit, int advancedFindStartupMode, int timeZoneCode,
            int helpLanguageId, int uiLanguageId, int defaultCalendarView, bool? isSendAsAllowed)
        {
            Trace($"Updating the settings of user {userId}");

            Service.Update(Utility.BuildUserSettings(userId, pagingLimit, advancedFindStartupMode, timeZoneCode,
                helpLanguageId, uiLanguageId, defaultCalendarView, isSendAsAllowed));
        }

        /// <summary>One column of the organization record.</summary>
        public static QueryExpression OrganizationSettingQuery(string attributeName)
        {
            var query = new QueryExpression(EntityNames.Organization)
            {
                ColumnSet = new ColumnSet(attributeName),
                TopCount = 1
            };
            query.AddOrder(AttributeNames.Name, OrderType.Ascending);

            return query;
        }

        /// <summary>Active queue items in a queue, newest first; optionally only those not assigned to a worker.</summary>
        /// <param name="top">Maximum number of records; 0 or less means no limit.</param>
        public static QueryExpression QueueItemsQuery(Guid queueId, bool onlyUnassigned, int top = 0)
        {
            var query = new QueryExpression(EntityNames.QueueItem)
            {
                ColumnSet = new ColumnSet(AttributeNames.EnteredOn, AttributeNames.ObjectTypeCode, AttributeNames.ObjectId, AttributeNames.QueueId)
            };
            query.AddOrder(AttributeNames.EnteredOn, OrderType.Descending);
            query.Criteria.AddCondition(AttributeNames.StateCode, ConditionOperator.Equal, 0);

            if (onlyUnassigned)
            {
                query.Criteria.AddCondition(AttributeNames.WorkerId, ConditionOperator.Null);
            }

            query.Criteria.AddCondition(AttributeNames.QueueId, ConditionOperator.Equal, queueId);

            if (top > 0)
            {
                query.TopCount = top;
            }

            return query;
        }

        /// <summary>
        /// The number of items in a queue, optionally only those no one is working on.
        /// </summary>
        public int CountQueueItems(Guid queueId, bool onlyUnassigned)
        {
            return CountRecords(QueueItemsQuery(queueId, onlyUnassigned));
        }

        /// <summary>
        /// The value of an environment variable: its current value when one is set, otherwise its default value.
        /// </summary>
        /// <param name="schemaName">Schema name of the environment variable, e.g. "new_ApiUrl".</param>
        /// <returns>The value, or null when there is no such variable or it has neither a current nor a default value.</returns>
        public string GetEnvironmentVariable(string schemaName)
        {
            var definition = RetrieveFirst(EnvironmentVariableQuery(schemaName.Trim()));

            if (definition == null)
            {
                Trace($"Environment variable {schemaName} was not found.");

                return null;
            }

            var currentValue = definition.GetAttributeValue<AliasedValue>(EnvironmentVariableValueAlias)?.Value as string;

            if (currentValue != null)
            {
                Trace($"Environment variable {schemaName} has a current value.");

                return currentValue;
            }

            Trace($"Environment variable {schemaName} uses its default value.");

            return definition.GetAttributeValue<string>(AttributeNames.DefaultValue);
        }

        /// <summary>Alias of the current-value link in <see cref="EnvironmentVariableQuery"/>.</summary>
        public const string EnvironmentVariableValueAlias = "currentvalue." + AttributeNames.Value;

        /// <summary>
        /// The environment variable definition with this schema name, with its default value and, through an outer
        /// link, its current value (aliased <see cref="EnvironmentVariableValueAlias"/>).
        /// </summary>
        public static QueryExpression EnvironmentVariableQuery(string schemaName)
        {
            var query = new QueryExpression(EntityNames.EnvironmentVariableDefinition)
            {
                ColumnSet = new ColumnSet(AttributeNames.DefaultValue),
                TopCount = 1
            };
            query.Criteria.AddCondition(AttributeNames.SchemaName, ConditionOperator.Equal, schemaName);

            var currentValue = query.AddLink(EntityNames.EnvironmentVariableValue, AttributeNames.EnvironmentVariableDefinitionId,
                AttributeNames.EnvironmentVariableDefinitionId, JoinOperator.LeftOuter);
            currentValue.EntityAlias = "currentvalue";
            currentValue.Columns = new ColumnSet(AttributeNames.Value);

            return query;
        }
    }
}
