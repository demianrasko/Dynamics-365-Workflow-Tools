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
