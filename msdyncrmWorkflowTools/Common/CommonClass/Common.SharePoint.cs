using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
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
            return Service.RetrieveMultiple(Queries.SharepointDocumentLocations(regardingObjectId));
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
    }
}
