using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
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
        [ReferenceTarget(EntityNames.Workflow)]
        public InArgument<EntityReference> Process { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var recordUrl = ClonningRecordURL.Get(executionContext);

            if (string.IsNullOrEmpty(recordUrl))
            {
                throw new InvalidPluginExecutionException("Record URL is required.");
            }

            common.SetProcess(common.GetRecordReference(recordUrl), Process.Get(executionContext));
        }
    }
}
