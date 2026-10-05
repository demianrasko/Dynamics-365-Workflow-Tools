using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
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
            var queueItems = Service.RetrieveMultiple(Queries.QueueItems(queueId, onlyUnassigned: true, top: Math.Max(quantity, 1))).Entities;

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
            var organization = Service.RetrieveMultiple(Queries.OrganizationSetting(attributeName)).Entities.FirstOrDefault();

            return organization?.GetAttributeValue<object>(attributeName);
        }

        /// <summary>
        /// Sets an organization setting; the value is stored as a whole number, true/false or text
        /// (see <see cref="Utility.ConvertSettingValue"/>).
        /// </summary>
        /// <returns>False when the organization record could not be read.</returns>
        public bool SetOrganizationSetting(string attributeName, string value)
        {
            var organization = Service.RetrieveMultiple(Queries.OrganizationSetting(attributeName)).Entities.FirstOrDefault();

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
    }
}
