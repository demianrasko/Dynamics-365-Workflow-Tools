using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class DeleteRecordAuditHistory : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"

            var _RecordURL = RecordURL.Get(executionContext);
            if (string.IsNullOrEmpty(_RecordURL))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }
            var parsedUrl = common.ParseRecordUrl(_RecordURL);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var entityName = parsedUrl.EntityName;
            var objectId = parsedUrl.Id;
            common.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");

            #endregion

            #region "DeleteRecordAuditHistory"

            common.DeleteRecordAuditHistory(entityName,objectId);

            #endregion
        }
    }
}
