using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Set Process Stage")]
    public class SetProcessStage : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> ClonningRecordURL { get; set; }

        [Input("Process")]
        [ReferenceTarget(EntityNames.Workflow)]
        public InArgument<EntityReference> Process { get; set; }

        [Input("Process Stage Name")]
        public InArgument<string> ProcessStage { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var process = Process.Get(executionContext) ?? throw new InvalidPluginExecutionException("Process is required.");
            var stageName = ProcessStage.Get(executionContext);

            if (string.IsNullOrEmpty(stageName))
            {
                throw new InvalidPluginExecutionException("Process Stage Name is required.");
            }

            var parsedUrl = common.ParseRecordUrl(ClonningRecordURL.Get(executionContext));
            common.Trace($"EntityName={parsedUrl.EntityName}--Id={parsedUrl.Id}");

            common.SetProcessStage(parsedUrl.ToEntityReference(), process, stageName);
        }
    }
}
