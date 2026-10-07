using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace msdyncrmWorkflowTools
{
    public partial class Common
    {
        /// <summary>
        /// Parses a record URL and fills in its entity name: from the URL's "etn" parameter when it has one,
        /// otherwise by looking up the "etc" type code in the metadata.
        /// </summary>
        /// <param name="recordUrl">The record URL a workflow passes in.</param>
        /// <returns>The object type code, record id and entity logical name.</returns>
        public RecordUrl ParseRecordUrl(string recordUrl)
        {
            var parsedUrl = Utility.ParseRecordUrl(recordUrl);

            if (!string.IsNullOrEmpty(parsedUrl.EntityName))
            {
                return parsedUrl;
            }

            return new RecordUrl(parsedUrl.ObjectTypeCode, parsedUrl.Id, GetEntityNameFromCode(parsedUrl.ObjectTypeCode));
        }

        /// <summary>
        /// The record URL of any record, built from a reference record URL of the same environment, the record's id as
        /// text and its table, for the Get Record URL activity.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The reference URL or the table is empty, or the id isn't a GUID.</exception>
        public string GetRecordUrl(string referenceRecordUrl, string recordId, string entityName)
        {
            if (string.IsNullOrEmpty(referenceRecordUrl) || string.IsNullOrEmpty(entityName))
            {
                throw new InvalidPluginExecutionException("Reference Record URL and Entity Logical Name are required.");
            }

            var id = Utility.RequiredGuid(recordId, "Record ID");
            var recordUrl = Utility.BuildRecordUrl(referenceRecordUrl, GetEntityTypeCode(entityName), entityName, id);
            Trace($"Record URL: {recordUrl}");

            return recordUrl;
        }

        /// <summary>
        /// The Dynamics 365 mobile app links for the record a record URL points at and its table, for the Entity
        /// Mobile Deep Link activity.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">The record URL is empty.</exception>
        public MobileDeepLinks GetMobileDeepLinks(string recordUrl)
        {
            var record = GetRecordReference(recordUrl, "Record URL");
            var links = new MobileDeepLinks(record.LogicalName, record.Id);
            Trace($"MobileDeepLinkEdit: {links.Edit}, MobileDeepLinkNew: {links.New}, MobileDeepLinkDefaultView: {links.DefaultView}");

            return links;
        }

        /// <summary>
        /// The record a required record URL input points at.
        /// </summary>
        /// <param name="recordUrl">The input's value.</param>
        /// <param name="inputName">The input's name as the designer shows it, for the error when it's empty.</param>
        /// <exception cref="InvalidPluginExecutionException">The URL is empty, or isn't a record URL.</exception>
        public EntityReference GetRecordReference(string recordUrl, string inputName)
        {
            return GetRecordReference(Utility.Required(recordUrl, inputName));
        }

        /// <summary>
        /// The record a record URL points at, with the entity name looked up from the URL's type code.
        /// </summary>
        public EntityReference GetRecordReference(string recordUrl)
        {
            var parsedUrl = ParseRecordUrl(recordUrl);

            Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");

            return parsedUrl.ToEntityReference();
        }

        /// <summary>
        /// The id of a model-driven app, from its unique name.
        /// </summary>
        /// <exception cref="InvalidPluginExecutionException">There is no app with that unique name.</exception>
        public string GetAppModuleId(string appModuleUniqueName)
        {
            var query = new QueryExpression
            {
                EntityName = EntityNames.AppModule,
                ColumnSet = new ColumnSet(AttributeNames.AppModuleId, AttributeNames.UniqueName),
                Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression (AttributeNames.UniqueName, ConditionOperator.Equal, appModuleUniqueName)
                            }
                        }
            };

            var app = Service.RetrieveMultiple(query).Entities.FirstOrDefault()
                ?? throw new InvalidPluginExecutionException($"There is no app with the unique name '{appModuleUniqueName}'.");

            return app[AttributeNames.AppModuleId].ToString();
        }

        /// <summary>
        /// A record URL that opens the record in the given model-driven app.
        /// </summary>
        public string GetAppRecordUrl(string recordUrl, string appModuleUniqueName)
        {
            var appModuleId = GetAppModuleId(appModuleUniqueName);

            return $"{recordUrl}&appid={appModuleId}";
        }
    }
}
