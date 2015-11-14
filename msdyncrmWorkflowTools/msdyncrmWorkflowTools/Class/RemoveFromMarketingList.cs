using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            Common objCommon = new Common(executionContext);
            objCommon.tracingService.Trace("Load CRM Service from context --- OK");
            #endregion

            #region "Read Parameters"
            EntityReference marketingList = this.MarketingList.Get(executionContext);
            objCommon.tracingService.Trace(String.Format("marketingList: {0} ", marketingList.Id.ToString()));

            EntityReference account = this.account.Get(executionContext);

            EntityReference contact = this.contact.Get(executionContext);

            EntityReference lead = this.lead.Get(executionContext);

            #endregion

            Guid idToRemove = Guid.Empty;

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

            RemoveMemberListRequest removeRequest = new RemoveMemberListRequest();
            removeRequest.ListId = marketingList.Id;
            removeRequest.EntityId = idToRemove;
            RemoveMemberListResponse removeResponse = (RemoveMemberListResponse)objCommon.service.Execute(removeRequest);

        }
    }


}
