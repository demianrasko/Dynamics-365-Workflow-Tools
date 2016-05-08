using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
namespace msdyncrmWorkflowTools.Class
{
    public class AddMarketingListToCampaign : CodeActivity
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> MarketingList { get; set; }

        [RequiredArgument]
        [Input("Marketing Campaign")]
        [ReferenceTarget("campaign")]
        public InArgument<EntityReference> Campaign { get; set; }



        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference marketingList = this.MarketingList.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("marketingList: {0} ", marketingList.Id.ToString()));

            EntityReference campaign = this.Campaign.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("campaign: {0} ", campaign.Id.ToString()));


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
