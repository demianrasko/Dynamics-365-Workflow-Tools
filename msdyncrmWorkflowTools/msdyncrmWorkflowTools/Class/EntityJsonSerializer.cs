using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
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

            var parsedUrl = Utility.ParseRecordUrl(serializingRecordUrl);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var objectId = parsedUrl.Id;
            var entityName = common.GetEntityNameFromCode(objectTypeCode);

            common.Trace("ObjectTypeCode=" + objectTypeCode + "--ParentId=" + objectId);

            #endregion

            #region "Clone Execution"

            var retrievedObject =
                common.service.Retrieve(entityName, new Guid(objectId), new ColumnSet(allColumns: true));
            common.Trace("retrieved object OK");

            //var newEntity = new Entity(entityName);
            var primaryIdAttribute = string.Empty;
            var primaryNameAttribute = string.Empty;
            var attributesToClone = common.GetEntityAttributesToClone(entityName, ref primaryIdAttribute, ref primaryNameAttribute);

            var json = Utility.SerializeEntity(entityName, primaryIdAttribute, objectId, retrievedObject, attributesToClone);
            common.Trace("json object OK");
            OutputJson.Set(executionContext, json);

            #endregion
        }
    }
}
