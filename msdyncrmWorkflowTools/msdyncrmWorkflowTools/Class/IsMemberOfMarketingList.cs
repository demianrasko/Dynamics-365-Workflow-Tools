// Not in the Power Platform build: it needs Dynamics 365 tables (lead, list or salesliterature).
#if !POWERPLATFORM
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace msdyncrmWorkflowTools.Class
{
    [ActivityName("Is Member Of Marketing List")]
    public class IsMemberOfMarketingList : WorkflowActivityBase
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget(EntityNames.List)]
        public InArgument<EntityReference> MarketingList { get; set; }

        [Output("IsMemberOfMarketingList")]
        public OutArgument<bool> MemberOfMarketingList
        {
            get;
            set;
        }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common common)
        {
            var marketingList = MarketingList.Get(executionContext);
            common.Trace($"marketingList: {marketingList.Id}");

            var isMember = common.IsMemberOfMarketingList(marketingList.Id, common.Context.PrimaryEntityId);

            MemberOfMarketingList.Set(executionContext, isMember);
        }
    }
}
#endif
