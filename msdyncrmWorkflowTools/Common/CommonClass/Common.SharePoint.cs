using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        /// <summary>
        /// The SharePoint document locations of a record.
        /// </summary>
        /// <param name="regardingObjectId">The record whose document locations are returned.</param>
        public EntityCollection GetSharepointLocations(Guid regardingObjectId)
        {
            return Service.RetrieveMultiple(SharepointDocumentLocationsQuery(regardingObjectId));
        }

        /// <summary>
        /// The absolute SharePoint URL of the first document location of the record a record URL points at, for the
        /// Get SharePoint Location URL activity.
        /// </summary>
        /// <returns>The absolute URL, or "URL Not found" when the record has no document location.</returns>
        /// <exception cref="InvalidPluginExecutionException">The record URL is empty.</exception>
        public string GetSharepointLocationUrl(string recordUrl)
        {
            var recordId = Utility.ParseRecordUrl(Utility.Required(recordUrl, "Record URL")).Id;

            return GetAbsoluteUrlFromLocation(GetSharepointLocations(recordId));
        }

        /// <summary>
        /// The absolute SharePoint URL of the first document location in <paramref name="locations"/>.
        /// </summary>
        /// <param name="locations">Document locations, e.g. from <see cref="GetSharepointLocations"/>.</param>
        /// <returns>The absolute URL, or "URL Not found" when there are no locations.</returns>
        public string GetAbsoluteUrlFromLocation(EntityCollection locations)
        {
            if (locations.Entities.Count == 0)
            {
                return "URL Not found";
            }

            var request = new RetrieveAbsoluteAndSiteCollectionUrlRequest
            {
                Target = locations[0].ToEntityReference()
            };

            var response = (RetrieveAbsoluteAndSiteCollectionUrlResponse)Service.Execute(request);

            Trace($"Absolute URL of document location record is '{response.AbsoluteUrl}'.");

            return response.AbsoluteUrl;
        }

        /// <summary>
        /// SharePoint document locations whose regarding record is <paramref name="regardingObjectId"/>.
        /// </summary>
        public static QueryExpression SharepointDocumentLocationsQuery(Guid regardingObjectId)
        {
            var query = new QueryExpression(EntityNames.SharePointDocumentLocation)
            {
                ColumnSet = new ColumnSet(AttributeNames.AbsoluteUrl, AttributeNames.SharePointDocumentLocationId, AttributeNames.RelativeUrl)
            };

            query.Criteria.AddCondition(AttributeNames.RegardingObjectId, ConditionOperator.Equal, regardingObjectId);

            return query;
        }
    }
}
