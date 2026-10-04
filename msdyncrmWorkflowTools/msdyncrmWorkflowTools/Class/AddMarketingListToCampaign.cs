// Not in the Power Platform build: it needs Dynamics 365 tables (lead, list or salesliterature).
#if !POWERPLATFORM
using Microsoft.Crm.Sdk.Messages;
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
            #region "Read Parameters"
            var marketingList = MarketingList.Get(executionContext);
            common.Trace($"marketingList: {marketingList.Id.ToString()} ");

            var campaign = Campaign.Get(executionContext);
            common.Trace($"campaign: {campaign.Id.ToString()} ");

            #endregion

            var request = new AddItemCampaignRequest
            {
                CampaignId = campaign.Id,
                EntityId = marketingList.Id,
                EntityName = EntityNames.List,
            };

            common.Service.Execute(request);
        }
    }
}
#endif
