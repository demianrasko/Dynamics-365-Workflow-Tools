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
            var target = DeleteUsingRecordURL.Get(executionContext)
                ? GetTargetFromUrl(executionContext, common)
                : GetTargetFromNameAndGuid(executionContext);

            common.DeleteRecord(target);
        }

        private EntityReference GetTargetFromUrl(CodeActivityContext executionContext, Common common)
        {
            var recordUrl = DeleteRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("ERROR: Delete Record URL to be deleted missing.");
            }

            return common.GetRecordReference(recordUrl);
        }

        private EntityReference GetTargetFromNameAndGuid(CodeActivityContext executionContext)
        {
            var entityTypeName = EntityTypeName.Get(executionContext);
            var entityGuid = EntityGuid.Get(executionContext);

            if (string.IsNullOrEmpty(entityTypeName) || string.IsNullOrEmpty(entityGuid))
            {
                throw new InvalidPluginExecutionException("ERROR: Entity Type name or GUID to be deleted missing.");
            }

            if (!Guid.TryParse(entityGuid, out var id))
            {
                throw new InvalidPluginExecutionException($"ERROR: Entity Guid '{entityGuid}' is not a valid GUID.");
            }

            return new EntityReference(entityTypeName, id);
        }
    }
}
