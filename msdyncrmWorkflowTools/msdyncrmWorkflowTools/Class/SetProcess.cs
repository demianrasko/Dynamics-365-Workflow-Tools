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

            var parsedUrl = common.ParseRecordUrl(cloningRecordUrl);
            //var objectTypeCode = parsedUrl.ObjectTypeCode;
            //var objectId = parsedUrl.Id;
            //var entityName = parsedUrl.EntityName;

            common.Trace($"ObjectTypeCode={parsedUrl.EntityName}--ParentId={parsedUrl.Id}");

            var process = Process.Get(executionContext);

            #endregion

            #region "SetProcess Execution"

            var request = new SetProcessRequest
            {
                Target = new EntityReference(parsedUrl.EntityName, new Guid(parsedUrl.Id)),
                NewProcess = process
            };

            common.Service.Execute(request);

            #endregion
        }
    }
}
