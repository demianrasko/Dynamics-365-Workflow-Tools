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
        [ReferenceTarget("list")]
        public InArgument<EntityReference> MarketingList { get; set; }

        [RequiredArgument]
        [Input("Marketing Campaign")]
        [ReferenceTarget("campaign")]
        public InArgument<EntityReference> Campaign { get; set; }

        protected override void ExecuteActivity(CodeActivityContext executionContext, Common objCommon)
        {
            #region "Read Parameters"
            var marketingList = MarketingList.Get(executionContext);
            objCommon.Trace($"marketingList: {marketingList.Id.ToString()} ");

            var campaign = Campaign.Get(executionContext);
            objCommon.Trace($"campaign: {campaign.Id.ToString()} ");

            #endregion
           
            var request = new AddItemCampaignRequest
            {
                CampaignId = campaign.Id,
                EntityId = marketingList.Id,
                EntityName = "list",
            };

            objCommon.service.Execute(request);
        }
    }
}
