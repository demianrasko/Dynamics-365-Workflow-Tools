using System;
using System.Activities;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace msdyncrmWorkflowTools.Class
{
    public class RemoveFromMarketingList : CodeActivity
    {
        [RequiredArgument]
        [Input("Marketing List")]
        [ReferenceTarget("list")]
        public InArgument<EntityReference> MarketingList { get; set; }

        [Input("Account")]
        [ReferenceTarget("account")]
        public InArgument<EntityReference> account { get; set; }

        [Input("Contact")]
        [ReferenceTarget("contact")]
        public InArgument<EntityReference> contact { get; set; }

        [Input("Lead")]
        [ReferenceTarget("lead")]
        public InArgument<EntityReference> lead { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            #region "Load CRM Service from context"

            var objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            var marketingList = MarketingList.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("marketingList: {0} ", marketingList.Id.ToString()));

            var account = this.account.Get(executionContext);

            var contact = this.contact.Get(executionContext);

            var lead = this.lead.Get(executionContext);

            #endregion

            var idToRemove = Guid.Empty;

            if (account != null)
            {
                idToRemove = account.Id;
            }
            else if (contact != null)
            {
                idToRemove = contact.Id;
            }
            else if (lead != null)
            {
                idToRemove = lead.Id;
            }
            objCommon.tracingService.Trace(String.Format("idToRemove: {0} ", idToRemove.ToString()));

            var removeRequest = new RemoveMemberListRequest();
            removeRequest.ListId = marketingList.Id;
            removeRequest.EntityId = idToRemove;
            var removeResponse = (RemoveMemberListResponse)objCommon.service.Execute(removeRequest);

        }
    }


}
