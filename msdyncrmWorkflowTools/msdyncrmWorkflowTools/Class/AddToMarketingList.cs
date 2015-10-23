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
    public class AddToMarketingList : CodeActivity
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

            Guid idToAdd = Guid.Empty;

            if (account != null)
            {
                idToAdd = account.Id;
            }
            else if (contact != null)
            {
                idToAdd = contact.Id;
            }
            else if (lead != null)
            {
                idToAdd = lead.Id;
            }
            objCommon.tracingService.Trace(String.Format("idToAdd: {0} ", idToAdd.ToString()));

            AddMemberListRequest addRequest = new AddMemberListRequest();
            addRequest.ListId = marketingList.Id;
            addRequest.EntityId = idToAdd;
            AddMemberListResponse addResponse = (AddMemberListResponse)objCommon.service.Execute(addRequest);
            
        }
        
    }
}
