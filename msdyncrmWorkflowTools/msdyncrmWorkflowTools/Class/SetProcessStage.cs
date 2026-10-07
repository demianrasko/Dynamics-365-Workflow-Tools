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
            common.SetProcessStage(common.GetRecordReference(ClonningRecordURL.Get(executionContext), "Record URL"),
                Utility.Required(Process.Get(executionContext), "Process"), Utility.Required(ProcessStage.Get(executionContext), "Process Stage Name"));
        }
    }
}
