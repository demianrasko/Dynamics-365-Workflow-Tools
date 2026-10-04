using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    public class EntityJsonSerializer : WorkflowActivityBase
    {
        #region "Parameter Definition"

        [RequiredArgument]
        [Input("Serializing Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> SerializingRecordURL { get; set; }

        [Output("Output Json")] public OutArgument<string> OutputJson { get; set; }
        #endregion

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"

            var serializingRecordUrl = SerializingRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(serializingRecordUrl))
            {
                throw new InvalidPluginExecutionException("Serializing Record URL is required.");
            }

            var parsedUrl = common.ParseRecordUrl(serializingRecordUrl);

            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");
            #endregion

            #region "Clone Execution"

            var retrievedObject =
                common.Service.Retrieve(parsedUrl.EntityName, parsedUrl.Id, new ColumnSet(allColumns: true));
            common.Trace("retrieved object OK");

            var primaryIdAttribute = string.Empty;
            var primaryNameAttribute = string.Empty;
            var attributesToClone = common.GetEntityAttributesToClone(parsedUrl.EntityName, ref primaryIdAttribute, ref primaryNameAttribute);

            var json = Utility.SerializeEntity(parsedUrl.EntityName, primaryIdAttribute, parsedUrl.Id, retrievedObject, attributesToClone);
            common.Trace("json object OK");
            OutputJson.Set(executionContext, json);
            #endregion
        }
    }
}
