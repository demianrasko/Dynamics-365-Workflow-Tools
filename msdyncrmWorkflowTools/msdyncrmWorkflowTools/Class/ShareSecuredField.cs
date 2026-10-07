using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools
{
    [ActivityName("Share Secured Field")]
    public class ShareSecuredField : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Record URL")]
        [ReferenceTarget("")]
        public InArgument<string> RecordURL { get; set; }

        [RequiredArgument]
        [Input("Attribute Name")]
        public InArgument<string> AttributeName { get; set; }

        [Input("Share With User")]
        [ReferenceTarget(EntityNames.SystemUser)]
        public InArgument<EntityReference> UserToShare { get; set; }

        [Input("Share With Team")]
        [ReferenceTarget(EntityNames.Team)]
        public InArgument<EntityReference> TeamToShare { get; set; }

        [RequiredArgument]
        [Input("Allow Read")]
        [Default("true")]
        public InArgument<bool> AllowRead { get; set; }

        [RequiredArgument]
        [Input("Allow Update")]
        [Default("true")]
        public InArgument<bool> AllowUpdate { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.ShareSecuredField(
                common.GetRecordReference(RecordURL.Get(executionContext), "Record URL"),
                AttributeName.Get(executionContext),
                AllowRead.Get(executionContext),
                AllowUpdate.Get(executionContext),
                UserToShare.Get(executionContext),
                TeamToShare.Get(executionContext));
        }
    }
}
