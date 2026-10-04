// Not in the Power Platform build: it needs Dynamics 365 tables (lead, list or salesliterature).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class AddMarketingListToCampaign : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget(EntityNames.List)]
        public InArgument<EntityReference> MarketingList { get; set; }

        [RequiredArgument]
        [Input("Marketing Campaign")]
        [ReferenceTarget(EntityNames.Campaign)]
        public InArgument<EntityReference> Campaign { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            common.AddListToCampaign(MarketingList.Get(executionContext).Id, Campaign.Get(executionContext).Id);
        }
    }
}
#endif
