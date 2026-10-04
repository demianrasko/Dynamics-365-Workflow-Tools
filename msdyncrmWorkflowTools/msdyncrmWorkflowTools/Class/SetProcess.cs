using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class SetProcess : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> ClonningRecordURL { get; set; }

        [Input("Process")]
        [ReferenceTarget("workflow")]
        public InArgument<EntityReference> Process { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            #region "Read Parameters"
            var cloningRecordUrl = ClonningRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(cloningRecordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }

            var parsedUrl = Utility.ParseRecordUrl(cloningRecordUrl);
            var objectTypeCode = parsedUrl.ObjectTypeCode;
            var objectId = parsedUrl.Id;
            var entityName = common.GetEntityNameFromCode(objectTypeCode);

            common.Trace($"ObjectTypeCode={objectTypeCode}--ParentId={objectId}");

            var process = Process.Get(executionContext);

            #endregion

            #region "SetProcess Execution"

            var request = new SetProcessRequest
            {
                Target = new EntityReference(entityName, new Guid(objectId)),
                NewProcess = process
            };

            common.service.Execute(request);

            #endregion
        }
    }
}
