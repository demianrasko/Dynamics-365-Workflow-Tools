using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class DeleteRecord : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Delete Using Record URL")]
        [Default("True")]
        public InArgument<bool> DeleteUsingRecordURL { get; set; }

        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> DeleteRecordURL { get; set; }

        [Input("Entity Type Name")]
        [ReferenceTarget("")]
        public InArgument<string> EntityTypeName { get; set; }

        [Input("Entity Guid")]
        [ReferenceTarget("")]
        public InArgument<string> EntityGuid { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var deleteRecordUrl = DeleteRecordURL.Get(executionContext);
            var entityName = string.Empty;
            var objectId = string.Empty;

            if (deleteRecordUrl != null)
            {
                var parsedUrl = Utility.ParseRecordUrl(deleteRecordUrl);
                var objectTypeCode = parsedUrl.ObjectTypeCode;
                entityName = common.GetEntityNameFromCode(objectTypeCode);
                objectId = parsedUrl.Id;
                common.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");
            }

            var deleteUsingRecordUrl = DeleteUsingRecordURL.Get(executionContext);
            var entityTypeName = EntityTypeName.Get(executionContext);
            var entityGuid = EntityGuid.Get(executionContext);

            #endregion

            #region "Delete Record Execution"

            if (deleteUsingRecordUrl)
            {
                common.Trace("Deleting record by URL: {0}", deleteRecordUrl);

                if (string.IsNullOrEmpty(deleteRecordUrl) )
                {
                    throw new InvalidPluginExecutionException("ERROR: Delete Record URL to be deleted missing.");
                }
                common.Service.Delete(entityName, new Guid (objectId));
            }
            else
            {
                common.Trace($"Record type to be deleted: {entityTypeName} and ID:{entityGuid}");
                if (string.IsNullOrEmpty(entityTypeName) || string.IsNullOrEmpty(entityGuid))
                {
                    throw new InvalidPluginExecutionException("ERROR: Entity Type name or GUID to be deleted missing.");
                }

                common.Trace("Deleting record by Guid: {0}-{1}", entityTypeName, entityGuid);
                common.Service.Delete(entityTypeName, new Guid (entityGuid));
            }

            #endregion
        }
    }
}
