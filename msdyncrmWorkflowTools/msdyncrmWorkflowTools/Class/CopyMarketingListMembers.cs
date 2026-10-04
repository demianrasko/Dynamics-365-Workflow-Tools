// Not in the Power Platform build: it needs Dynamics 365 tables (lead, list or salesliterature).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class CopyMarketingListMembers : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Source List")]
        [ReferenceTarget(EntityNames.List)]
        public InArgument<EntityReference> SourceList { get; set; }

        [RequiredArgument]
        [Input("Target List")]
        [ReferenceTarget(EntityNames.List)]
        public InArgument<EntityReference> TargetList { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.CopyListMembers(SourceList.Get(executionContext).Id, TargetList.Get(executionContext).Id);
        }
    }
}
#endif
