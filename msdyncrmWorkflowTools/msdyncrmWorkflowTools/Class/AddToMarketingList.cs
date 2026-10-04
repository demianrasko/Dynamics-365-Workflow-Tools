// Not in the Power Platform build: it needs Dynamics 365 tables (lead, list or salesliterature).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    public class AddToMarketingList : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> MarketingList { get; set; }

        [Input("Account")]
        [ReferenceTarget("account")]
        // ReSharper disable once InconsistentNaming
        public InArgument<EntityReference> account { get; set; }

        [Input("Contact")]
        [ReferenceTarget("contact")]
        // ReSharper disable once InconsistentNaming
        public InArgument<EntityReference> contact { get; set; }

        [Input("Lead")]
        [ReferenceTarget("lead")]
        // ReSharper disable once InconsistentNaming
        public InArgument<EntityReference> lead { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var marketingList = MarketingList.Get(executionContext);
            var member = Utility.GetMarketingListMember(account.Get(executionContext), contact.Get(executionContext), lead.Get(executionContext));

            common.AddToMarketingList(marketingList.Id, member);
        }
    }
}
#endif
